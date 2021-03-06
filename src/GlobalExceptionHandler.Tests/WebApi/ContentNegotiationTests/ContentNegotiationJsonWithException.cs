﻿using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GlobalExceptionHandler.ContentNegotiation.Mvc;
using GlobalExceptionHandler.Tests.Exceptions;
using GlobalExceptionHandler.Tests.Fixtures;
using GlobalExceptionHandler.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace GlobalExceptionHandler.Tests.WebApi.ContentNegotiationTests
{
    public class ContentNegotiationJsonWithException : IClassFixture<WebApiServerFixture>
    {
        private const string ApiProductNotFound = "/api/productnotfound";
        private readonly HttpRequestMessage _requestMessage;
        private readonly HttpClient _client;

        public ContentNegotiationJsonWithException(WebApiServerFixture fixture)
        {
            // Arrange
            var webHost = fixture.CreateWebHostWithMvc();
            webHost.Configure(app =>
            {
                app.UseGlobalExceptionHandler(x =>
                {
                    x.Map<RecordNotFoundException>().ToStatusCode(StatusCodes.Status404NotFound)
                        .WithBody(e => new TestResponse
                        {
                            Message = "An exception occured"
                        });
                });

                app.Map(ApiProductNotFound, config =>
                {
                    config.Run(context => throw new RecordNotFoundException("Record could not be found"));
                });
            });

            // Act
            _requestMessage = new HttpRequestMessage(new HttpMethod("GET"), ApiProductNotFound);
            _requestMessage.Headers.Accept.Clear();
            _requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _client = new TestServer(webHost).CreateClient();
        }
        
        [Fact]
        public async Task Returns_correct_response_type()
        {
            var response = await _client.SendAsync(_requestMessage);
            response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
        }

        [Fact]
        public async Task Returns_correct_status_code()
        {
            var response = await _client.SendAsync(_requestMessage);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Returns_correct_body()
        {
            var response = await _client.SendAsync(_requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            content.ShouldContain("{\"message\":\"An exception occured\"}");
        }
    }
}