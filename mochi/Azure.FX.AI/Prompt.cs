using Azure.AI.OpenAI;

namespace Azure.FX.AI;

public class Prompt
{
    ChatCompletionsOptions _options = new ChatCompletionsOptions();

    public ChatCompletionsOptions CreateChatOptions() => _options;
    
    public Prompt(string systemMessage)
    {
        var message = new ChatMessage(ChatRole.System, systemMessage);
        _options.Messages.Add(message);
    }
    
    internal void SetSystemMessage(string systemMessage)
    {
        var message = new ChatMessage(ChatRole.System, systemMessage);
        _options.Messages[0] = message;
    }

    public void Add(string message)
    {
        var m = new ChatMessage(ChatRole.User, message);
        _options.Messages.Add(m);
    }

    public void Add(string message, ChatRole role)
    {
        var m = new ChatMessage(role, message);
        _options.Messages.Add(m);
    }
}
