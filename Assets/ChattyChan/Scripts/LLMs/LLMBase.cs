using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;

namespace LLMs
{
    public abstract class LLMBase: MonoBehaviour
    {
        
        /// <summary>
        /// api地址
        /// </summary>
        [InspectorName("API地址")]
        public string url;
        

        public string systemPrompt;
        
        

        [Header("上下文设置")]
        
        [InspectorName("历史消息保留数量")]
        [SerializeField] protected int historyKeepCount = 15;
        [InspectorName("历史消息保留 Token")]
        [SerializeField] protected int historyTokenCount = 3000;
        
        
        
        /// <summary>
        /// 计算方法调用的时间
        /// </summary>
        protected Stopwatch stopwatch = new Stopwatch();



        /// <summary>
        /// 发送消息
        /// </summary>
        public virtual void PostMsg(List<SendData> message, Action<string> callback) 
        {
            var data = StuffData(message);
            
            StartCoroutine(Request(data, callback));
        }
        
        
        public virtual IEnumerator Request(List<SendData> sendData, System.Action<string> callback)
        {
            yield return new WaitForEndOfFrame();
          
        }


        /// <summary>
        /// 钳制历史消息
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        public List<SendData> StuffData(List<SendData> dataList)
        {
            List<SendData> result = new List<SendData>();
            int tokenCount = 0;
            
            // SystemPrompt常驻
            result.Add(new SendData("system", systemPrompt));

            if (dataList == null)
                return result;
            
            // 倒叙遍历datalist，不断取出最后一个元素，直到tokenCount超过m_HistoryTokenCount
            for (var i = dataList.Count - 1; i >= 0; i--)
            {
                var data = dataList[i];
                tokenCount += CalcToken(data);
                if (tokenCount > historyTokenCount)
                {
                    break;
                }
                result.Insert(1, data);
            }
            
            return result;
        }

        /// <summary>
        /// 简单计算 token 数
        /// 不使用真实 token
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private int CalcToken(SendData message)
        {
            return message.content.Length;
        }
        

        [Serializable]
        public class SendData
        {
            [SerializeField] public string role;
            [SerializeField] public string content;
            public SendData() { }
            public SendData(string _role, string _content)
            {
                role = _role;
                content = _content;
            }

        }

    }
}