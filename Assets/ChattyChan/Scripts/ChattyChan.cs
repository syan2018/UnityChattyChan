using System;
using System.Collections.Generic;
using LLMs;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 用于管理ChattyChan这一角色
/// </summary>
public class ChattyChan: MonoBehaviour
{
    
    /// <summary>
    /// 聊天配置
    /// </summary>
    public LLMBase ChatModel;
        
        
    /// <summary>
    /// 调整 System Prompt
    /// 会随游戏进程改变
    /// 调整AI人设就用这个
    /// </summary>
    public string SystemPrompt = "现在，你是一位AI猫娘，我希望你的每一句回复都调用FunctionCall，并附上可爱的表情！";
    
    /// <summary>
    /// 语言
    /// </summary>
    [SerializeField] protected string Language = "中文";
    
    /// <summary>
    /// 回头要继续修 Prompt
    /// </summary>
    private string CompleteSystemPrompt =>
        " 当前为角色的人物设定：" + SystemPrompt +
        " 请使用该语言进行回答：" + Language;
    
    /// <summary>
    /// 差分角色动画表现
    /// </summary>
    public Animator Animator;
    
    /// <summary>
    /// 差分支持的动画表现
    /// TODO: 依靠表格驱动状态
    /// </summary>
    public readonly List<string> Expressions = new List<string>()
    {
        "Idle", "Joyful", "Tearful", "Disgusted", "Shy",
        "Very Shy", "Angry", "Yandere Glare", "Yandere Smile",
        "Eyes Closed", "Sinister", "Tired"
    };

    /// <summary>
    /// 记录所有对话记录
    /// </summary>
    public readonly List<LLMBase.SendData> SendDataList = new List<LLMBase.SendData>();

    /// <summary>
    /// 聊天记录
    /// </summary>
    public List<string> ChatHistory => SendDataList.ConvertAll(data => data.content);
        
    /// <summary>
    /// 区别于真实聊天记录
    /// 在聊天记录较长时会进行快速总结，以节省Token和长期记忆
    /// TODO: 待实现
    /// </summary>
    public readonly List<string> SendHistory = new List<string>();
    
    /// <summary>
    /// 当前返回
    /// </summary>
    public string Response;

    private void Awake()
    {
        
        InitFunctions();
        
        // TODO: 干掉
        InitInput();

    }

    #region 定义驱动 Functions

    private void InitFunctions()
    {
        
        if (ChatModel is not ChatGptTurbo chatLlm)
            return;
        
        InitSpeechAndExpressionFunc();
        
    }

    /// <summary>
    /// 用于对话和驱动差分的方法
    /// </summary>
    private void InitSpeechAndExpressionFunc()
    {
        // 定义参数
        var expressionParameters = new GPTFunctionParameters
        {
            type = "object",
            properties = new Dictionary<string, GPTFunctionParameterProperties>
            {
                {
                    // 定义对话内容
                    "speech", new GPTFunctionParameterProperties
                    {
                        type = "string",
                        description = "Output the dialogue content to this parameter"
                    }
                },
                {
                    // 定义参数表情
                    "expression", new GPTFunctionParameterProperties
                    {
                        type = "string",
                        description = "Choose the expression to use, default to Idle",
                        Enum = Expressions
                    }
                },
            },
            required = new List<string> { "speech" }
        };

        // 定义Function
        var expressionFunction = new GPTFunction(
            _name: "talk_with_expression",
            _description: "When not invoking other Func, please use this Func to create dialogues with expressions",
            _parameters: expressionParameters,
            _callback: SpeechAndExpressionCallback
        );
        
        // 确定Type类型
        Functions[FuncTypes.Normal].Add(expressionFunction);
    }


    private readonly Dictionary<FuncTypes,List<GPTFunction>> Functions = new Dictionary<FuncTypes, List<GPTFunction>>()
    {
        {FuncTypes.Normal, new List<GPTFunction>()}
    };


    private enum FuncTypes
    {
        Normal,
    }

    private List<GPTFunction> GetFunctionsByTypes(FuncTypes key)
    {
        if (Functions.TryGetValue(key, out var functions))
        {
            return functions;
        }
        return null;
    }
    

    /// <summary>
    /// TODO: 可能在不同游戏进度下，使用不同 Function 列表
    /// 可能涉及不同FuncTypes的组合
    /// 但现在通用返回Anim
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public List<GPTFunction> GetFunctionsByState()
    {
        // TODO: 判断获得返回列表
        return GetFunctionsByTypes(FuncTypes.Normal);
        
    }
    
    #endregion
    
    

    /// <summary>
    /// 回调函数的输入是 FunctionCall['arguments']
    /// </summary>
    private void SpeechAndExpressionCallback(Dictionary<string, object> callback)
    {
        
        // 得到对话数据，
        if (callback.TryGetValue("speech", out var speech) && speech is string speechText)
        {
            GetResponse(speechText);

        }
        
        if (callback.TryGetValue("expression", out var expressionObj) && expressionObj is string expression)
        {
            if (Animator == null)
            {
                Debug.LogWarning("Animator is null, try to play animation: " + expression + " failed.");
                return;
            }
            Animator.SetTrigger(expression);
        }
        else
        {
            if (Animator == null)
            {
                return;
            }
            Animator.SetTrigger("Idle");
        }
        
        
    }
        
    
    /// <summary>
    /// 发送请求
    /// </summary>
    public void SendData(string msg)
    {
        // 依据当前状态更新SystemPrompt
        ChatModel.systemPrompt = SystemPrompt;

        SendDataList.Add(new LLMBase.SendData("user", msg));
        
        // TODO: 干掉
        OutputText.text = "正在思考中...";
        
        if (ChatModel is ChatGptTurbo turbo)
        {
            // TODO: 优化为记忆版本
            turbo.PostMsg(SendDataList, GetResponse, GetFunctionsByState());
            
        }
        else
        {
            ChatModel.PostMsg(SendDataList, GetResponse);
        }


    }

    /// <summary>
    /// 注：如果被function_call劫持则不会触发回调
    /// </summary>
    /// <param name="response"></param>
    private void GetResponse(string response)
    {
        Response = response.Trim();
        
        // TODO: 干掉
        OutputText.text = Response;
        
        Debug.Log("收到AI回复：" + Response);
        
        SendDataList.Add(new LLMBase.SendData("assistant", Response));
        
        CheckHistory();
        
    }

    // 如果对话过长，进行快速总结
    public void CheckHistory()
    {
        
    }
    
    
    

    #region UI测试临时
    
    [Header("输入输出测试")]


    public InputField InputWord;

    public Button CommitMsgBtn;
    
    public Text OutputText;
    
    private void InitInput()
    {
        CommitMsgBtn.onClick.AddListener(delegate
        {
            SendData(InputWord.text);
        });
    }
    

    #endregion
    
    
    
}