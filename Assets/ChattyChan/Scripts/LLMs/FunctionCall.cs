using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LLMs
{

    #region FunctionCall

    /// <summary>
    /// 当个参数的类型
    /// </summary>
    [Serializable]
    public class GPTFunctionParameterProperties
    {
        public string type; // 类型，如 "string"
        public string description; // 描述
        
        [JsonProperty("enum")]
        public List<string> Enum; // 适用于具有枚举值的类型

        public bool ShouldSerializeEnum() // 注意这里的方法名是 'ShouldSerialize' + 属性名
        {
            return Enum is { Count: > 0 }; // 当列表不为空时返回 true
        }
    }

    /// <summary>
    /// 方法的参数组定义
    /// </summary>
    [Serializable]
    public class GPTFunctionParameters
    {
        public string type; // 这可能总是 "object"，但包括它以保持一致性
        public Dictionary<string, GPTFunctionParameterProperties> properties; // 参数属性
        public List<string> required; // 必需的参数列表
    }

    /// <summary>
    /// 定义Function描述
    /// </summary>
    [Serializable]
    public class GPTFunction
    {
        public string name; // 函数名
        public string description; // 函数描述
        public GPTFunctionParameters parameters; // 参数结构
        
        [JsonIgnore]
        public Action<Dictionary<string, object>> callback; // 回调函数
        [JsonIgnore]
        public bool isNecessarily = false; // 是否必须
        
        [JsonIgnore]
        public Dictionary<string, string> forceFunctionCall; // 强制调用

        // 在构造函数中初始化所有内容
        public GPTFunction(string _name, string _description, GPTFunctionParameters _parameters, Action<Dictionary<string, object>> _callback)
        {
            name = _name;
            description = _description;
            parameters = _parameters;
            callback = _callback;

            if (!isNecessarily)
                return;
            
            forceFunctionCall = new Dictionary<string, string> { {"name", _name} };
            
        }
    }
    

    /// <summary>
    /// FunctionCall返回值解析
    /// </summary>
    [Serializable]
    public class FunctionCallResponse
    {
        public string name;
        
        public string arguments; // 将类型更改为 string
        
        [JsonIgnore]
        public Dictionary<string, object> Arguments => GetArgumentsAsDictionary();

        // 可以添加一个方法来解析 arguments 字段（如果它是一个 JSON 字符串的话）
        public Dictionary<string, object> GetArgumentsAsDictionary()
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(arguments);
            }
            return new Dictionary<string, object>();
        } 
    }
    
    #endregion
    
}