using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace LLMs
{
    public class ChatGptTurbo: LLMBase
    {
        public ChatGptTurbo()
        {
            url = "https://api.openai.com/v1/chat/completions";
        }
        
        
        [Header("GPT Turbo Setting")]
        
        [SerializeField] public string apiKey;
        
        [SerializeField] public string gptModel = "gpt-3.5-turbo-0613";
        
        

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <returns></returns>
        public override void PostMsg(List<SendData> message, Action<string> callback)
        {
            base.PostMsg(message, callback);
        }
        
        /// <summary>
        /// 带 functions 的消息传输
        /// </summary>
        public void PostMsg(List<SendData> message, Action<string> callback, List<GPTFunction> functions)
        {
            var data = StuffData(message);

            StartCoroutine(Request(data, callback, functions));
        }

        
        /// <summary>
        /// 带FunctionCall的请求
        /// </summary>
        public IEnumerator Request(List<SendData> sendData, System.Action<string> callback, List<GPTFunction> functions)
        {
            
            stopwatch.Restart();
            
            PostData postData = new PostData
            {
                model = gptModel,
                messages = sendData,
                Functions = functions,
            };

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                string jsonText = JsonConvert.SerializeObject(postData).Trim();
                
                Debug.Log(jsonText);
                
                byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonText);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", string.Format("Bearer {0}", apiKey));

                yield return request.SendWebRequest();

                // 等待返回值
                if (request.responseCode == 200)
                {
                    string msgBack = request.downloadHandler.text;
                    Debug.Log(msgBack);
                    
                    var textBack = JsonUtility.FromJson<MessageBack>(msgBack);
                    if (textBack != null && textBack.choices.Count > 0)
                    {
                        // 使用 function_call
                        if (textBack.choices[0].finish_reason == "function_call")
                        {
                            // 尝试 FunctionCall 回调
                            var functionCall = textBack.choices[0].message.function_call;
                        
                            Debug.Log(functionCall.name);
                            Debug.Log(functionCall.arguments);
                            
                            var function = functions.Find(f => f.name == functionCall.name);
                            if (function != null)
                                // 如果找到则走function的CallBack
                                function.callback(functionCall.Arguments);
                            else
                            {
                                Debug.LogError($"Function {functionCall.name} not found");
                            }
                        }
                        else
                        {
                            string textMsg = textBack.choices[0].message.content;
                            callback(textMsg);
                        }
                        
                    }

                }
                else
                {
                    string msgBack = request.downloadHandler.text;
                    Debug.LogError(msgBack);
                }

                stopwatch.Stop();
                Debug.Log("ChatGpt耗时："+ stopwatch.Elapsed.TotalSeconds);
            }
            
        }

        /// <summary>
        /// 调用纯文本接口
        /// </summary>
        public override IEnumerator Request(List<SendData> sendData, System.Action<string> callback)
        {
            stopwatch.Restart();
            
            PostData postData = new PostData
            {
                model = gptModel,
                messages = sendData,
            };
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                string jsonText = JsonConvert.SerializeObject(postData).Trim();
                
                Debug.Log(jsonText);
                
                byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonText);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", string.Format("Bearer {0}", apiKey));

                yield return request.SendWebRequest();

                if (request.responseCode == 200)
                {
                    string msgBack = request.downloadHandler.text;
                    Debug.Log(msgBack);
                    
                    MessageBack textBack = JsonUtility.FromJson<MessageBack>(msgBack);
                    if (textBack != null && textBack.choices.Count > 0)
                    {

                        string textMsg = textBack.choices[0].message.content;
                        
                        callback(textMsg);
                    }

                }
                else
                {
                    string msgBack = request.downloadHandler.text;
                    Debug.LogError(msgBack);
                }

                stopwatch.Stop();
                Debug.Log("ChatGpt耗时："+ stopwatch.Elapsed.TotalSeconds);
            }
            
        }

        #region 数据包定义

        [Serializable]
        public class PostData
        {
            [SerializeField] public string model;
            [SerializeField] public List<SendData> messages;
            [SerializeField] public float temperature = 0.7f;
            
            [JsonProperty("functions")]
            [SerializeField] public List<GPTFunction> Functions; // 可选，仅在使用functions时设置
            
            [JsonProperty("function_call")]
            [SerializeField] public Dictionary<string, string> FunctionCall;
            
            
            // 按照约定，该方法命名为 ShouldSerialize + 属性名
            public bool ShouldSerializeFunctions() 
            {
                return Functions is { Count: > 0 };
            }
            
            
            public bool ShouldSerializeFunctionCall() 
            {
                return FunctionCall is { Count: > 0 };
            }
        }

        [Serializable]
        public class MessageBack
        {
            public string id;
            public string created;
            public string model;
            public List<MessageBody> choices;
        }
        [Serializable]
        public class MessageBody
        {
            public Message message;
            public string finish_reason;
            public string index;
        }
        [Serializable]
        public class Message
        {
            public string role;
            public string content;
            public FunctionCallResponse function_call;
            
        }

        #endregion
        
        
    }


}