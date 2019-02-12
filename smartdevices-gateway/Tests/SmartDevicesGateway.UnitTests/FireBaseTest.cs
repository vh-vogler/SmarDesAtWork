//
// Copyright (c) Vogler Engineering GmbH. All rights reserved.
// Licensed under the MIT License. See LICENSE.md in the project root for license information.
//
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartDevicesGateway.Services.ConfigService;
using SmartDevicesGateway.Services.FcmService;
using SmartDevicesGateway.Services.FcmService.Requests;
using SmartDevicesGateway.Services.FcmService.Responses;
using SmartDevicesGateway.TestCommon;
using SmartDevicesGateway.UnitTests.Fixtures;
using Xunit;

namespace SmartDevicesGateway.UnitTests
{
    [Trait("Category", Categories.UNIT)]
    public class FireBaseTest : IClassFixture<ConfigFixture>
    {
        public ConfigFixture Fixture { get; }

        public FireBaseTest(ConfigFixture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        [Trait("Requires", "Firebase")]
        public void TestFirebase()
        {
            var section = Fixture.Configuration.GetSection("FcmConfig");
            Assert.NotNull(section);
            var clientToken = section["SampleAppToken"];
            Assert.NotNull(clientToken);
            Assert.NotEmpty(clientToken);

            var msg = new FcmMessageBuilder()
                .AddReceiver(clientToken)
                .SetDebug(false)
                .SetPriority(FcmMessagePriority.High)
                .SetData(new {foo = "bar"})
                .Build();

            var response = Send(msg);

            Assert.Equal(200, response.ErrorCode);
            Assert.NotNull(response.ResponseMessage);

            Assert.Equal(1, response.ResponseMessage.Success);
            Assert.Equal(0, response.ResponseMessage.Failure);
        }

        private static string ParseStringResponse(WebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                if (stream == null || !stream.CanRead)
                {
                    return null;
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private DownstreamMessageResponse ParseMessageResponse(WebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                if (stream == null || !stream.CanRead)
                {
                    return null;
                }

                using (var reader = new StreamReader(stream))
                {
                    var data = reader.ReadToEnd();
                    var messageResponse = JsonConvert.DeserializeObject<DownstreamMessageResponse>(data);
                    return messageResponse;
                }
            }
        }

        private IFcmResponse Send(FcmMessage message)
        {
            var section = Fixture.Configuration.GetSection("FcmConfig");
            Assert.NotNull(section);

            var serverKey = section["ServerKey"];
            var apiUrl = section["ApiUrl"];

            var fcmResponse = new FcmResponse();

            var request = (HttpWebRequest)WebRequest.Create(apiUrl);
            request.Method = "POST";
            request.Headers.Add("Authorization", "key=" + serverKey);
            request.ContentType = "application/json";

            var json = JsonConvert.SerializeObject(message);
            var byteArray = Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;

            //Write data to stream
            using (var dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (var response = (HttpWebResponse)request.GetResponseWithoutException())
            {
                //Evaluate Response
                var statusCode = (int)response.StatusCode;
                fcmResponse.ErrorCode = statusCode;

                if (statusCode >= 500 && statusCode <= 599)
                {
                    fcmResponse.ErrorMessage = ParseStringResponse(response);
                    fcmResponse.Error = ResponseError.InternalServerError;
                }
                else
                {
                    switch (statusCode)
                    {
                        case 401:
                            fcmResponse.ErrorMessage = ParseStringResponse(response);
                            fcmResponse.Error = ResponseError.AuthenticationError;
                            break;
                        case 400:
                            fcmResponse.ErrorMessage = ParseStringResponse(response);
                            fcmResponse.Error = ResponseError.InvalidJson;
                            break;
                        case 200:
                            fcmResponse.ResponseMessage = ParseMessageResponse(response);
                            fcmResponse.Error = ResponseError.NoError;
                            break;
                        default:
                            break;
                    }
                }
                return fcmResponse;
            }
        }

    }
}
