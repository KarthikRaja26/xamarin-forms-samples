﻿using ChatClient.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    public class SignalRService
    {
        HttpClient client;

        public delegate void MessageReceivedHandler(object sender, Message message);
        public event MessageReceivedHandler NewMessageReceived;
        public bool IsConnected { get; private set; }
        public bool IsBusy { get; private set; }

        public SignalRService()
        {
            client = new HttpClient();
        }

        public async Task SendMessageAsync(string username, string message)
        {
            IsBusy = true;

            var newMessage = new Message
            {
                Name = username,
                Text = message
            };

            var json = JsonConvert.SerializeObject(newMessage);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync($"{Constants.HostName}/api/talk", content);

            IsBusy = false;
        }

        public async Task ConnectAsync()
        {
            try
            {
                IsBusy = true;

                var negotiateJson = await client.GetStringAsync($"{Constants.HostName}/api/negotiate");
                var negotiate = JsonConvert.DeserializeObject<NegotiateInfo>(negotiateJson);
                var connection = new HubConnectionBuilder()
                    .WithUrl(negotiate.Url, options =>
                    {
                        options.AccessTokenProvider = async () => negotiate.AccessToken;
                    })
                    .Build();

                connection.On<JObject>(Constants.MessageName, AddNewMessage);
                await connection.StartAsync();

                IsConnected = true;
                IsBusy = false;
            }
            catch(Exception ex)
            {
                Debugger.Break();
            }
        }

        void AddNewMessage(JObject message)
        {
            var messageModel = new Message
            {
                Name = message.GetValue("name").ToString(),
                Text = message.GetValue("text").ToString(),
                TimeReceived = DateTime.Now
            };

            NewMessageReceived?.Invoke(this, messageModel);
        }

    }
}
