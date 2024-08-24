using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class CPHInline
{ 
    public bool Execute()
    {
        try
        {
            string apiKey = GetGlobalVariable<string>("chatGptApiKey", true);
            string gptModel = GetGlobalVariable<string>("chatGptModel", true);
            string gptBehavior = GetGlobalVariable<string>("chatGptBehavior", true);
            double gptTempValue = Convert.ToDouble("1");
            string messageInput = args["rawInput"].ToString();
            string user = args["userName"].ToString();
            List<string> exclusionList = GetGlobalVariable<List<string>>("chatGptExclusions", true);
            int messageCount = GetGlobalVariable<int>("chatGptMessageCount", true);

            if (exclusionList != null && exclusionList.Contains(user))
            {
                LogInfo($"User {user} is on the exclusion list. Skipping GPT processing.");
                return false;
            }

            messageInput = CleanMessageInput(messageInput);
            List<string> chatHistory = GetGlobalVariable<List<string>>("chatHistory", false) ?? new List<string>();
            chatHistory = chatHistory.Count > messageCount ? chatHistory.GetRange(chatHistory.Count - messageCount, messageCount) : chatHistory;

            ChatGptApiRequest chatGpt = new ChatGptApiRequest(apiKey, chatHistory, this);
            string response = chatGpt.GenerateResponse(messageInput, gptModel, gptBehavior, gptTempValue);
            ProcessAndSendResponse(response, user);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error in Execute method: {ex.Message}");
            return false;
        }
    }

    public bool ChatGptShoutouts()
    {        
        string targetUser = args["targetUser"].ToString();
        string targetDescription = args["targetDescription"].ToString();
        string targetGame = args["game"].ToString();
        string targetTags = args["tagsDelimited"].ToString();
        string userType = args["userType"].ToString();
        string targetLink = $"https://twitch.tv/{targetUser}";
        string prompt = $"Construct a witty message that doesn't exceed 400 characters, encouraging viewers to watch @{targetUser}'s stream. Ensure the response includes @{targetUser} and {targetLink}, and does NOT include hashtags";
        string apiKey = GetGlobalVariable<string>("chatGptApiKey", true);
        string gptModel = GetGlobalVariable<string>("chatGptModel", true);
        string gptBehaviorGlobal = GetGlobalVariable<string>("chatGptBehavior", true);
        string gptShoutoutAddon = $"build your response using information from the following data: {targetUser}, {targetDescription}, {targetGame}, {targetTags}";
        string gptBehavior = gptBehaviorGlobal + gptShoutoutAddon;
        double gptTempValue = Convert.ToDouble("1");
            
        ChatGptApiRequest chatGpt = new ChatGptApiRequest(apiKey, new List<string>(), this);
        string response;
        
        try
        {
            response = chatGpt.GenerateResponse(prompt, gptModel, gptBehavior, gptTempValue);
        }
        catch (Exception ex)
        {
            LogError($"ChatGPT ERROR: {ex.Message}");
            return false;
        }

        Root root = JsonConvert.DeserializeObject<Root>(response);
        string finalGpt = root.choices[0].message.content;
        finalGpt = finalGpt.Trim('\"');
        SetGlobalVariable("ChatGPT Response", finalGpt, false);
        if (userType == "twitch")
        {
            SendMessage(finalGpt, true);
            LogInfo("Sent SO message to Twitch");
        }
        else if (userType == "youtube")
        {
            SendYouTubeMessage("Sorry, ChatGPT shoutouts are only available to Twitch users at this time.", true);
            LogInfo("Sent message to YouTube-SO for Twitch only");
        }
        else if (userType == "trovo")
        {
            SendTrovoMessage("Sorry, ChatGPT shoutouts are only available to Twitch users at this time.", true);
            LogInfo("Sent message to Trovo-SO for Twitch only");
        }
        return true;
    }

    private void ProcessAndSendResponse(string response, string user)
    {
        Root root = JsonConvert.DeserializeObject<Root>(response);
        string myString = root.choices[0].message.content;
        LogInfo("GPT Response: " + myString);
        string finalGpt = CleanResponseString(myString);

        // Check and remove duplicate mentions
        finalGpt = RemoveDuplicateMentions(finalGpt, user);

        // Ensure the user is tagged in the answer
        if (!finalGpt.Contains($"@{user}"))
        {
            finalGpt = $"@{user} " + finalGpt;
        }

        SetGlobalVariable("ChatGPT Response", finalGpt, false);
        string userType = args["userType"].ToString();
        SendResponseToPlatform(finalGpt, userType);
    }

    private string CleanResponseString(string response)
    {
        string myStringCleaned0 = response.Replace(Environment.NewLine, " ");
        string mystringCleaned1 = Regex.Replace(myStringCleaned0, @"\r\n?|\n", " ");
        string myStringCleaned2 = Regex.Replace(mystringCleaned1, @"[\r\n]+", " ");
        string unescapedString = Regex.Unescape(myStringCleaned2);
        return unescapedString.Trim();
    }

    private string RemoveDuplicateMentions(string response, string user)
    {
        string pattern = $@"(@{user})(\s+\1)+";
        return Regex.Replace(response, pattern, "$1");
    }

    private void SendResponseToPlatform(string response, string userType)
    {
        if (userType == "twitch")
        {
            SendMessage(response, true);
            LogInfo("Sent message to Twitch");
        }
        else if (userType == "youtube")
        {
            SendYouTubeMessage(response, true);
            LogInfo("Sent message to YouTube");
        }
        else if (userType == "trovo")
        {
            SendTrovoMessage(response, true);
            LogInfo("Sent message to Trovo");
        }
    }

    public void LogInfo(string message)
    {
        CPH.LogInfo(message);
    }

    public void LogError(string message)
    {
        CPH.LogError(message);
    }

    public void SendMessage(string message, bool isWhisper)
    {
        CPH.SendMessage(message, isWhisper);
    }

    public void SendYouTubeMessage(string message, bool isWhisper)
    {
        CPH.SendYouTubeMessage(message, isWhisper);
    }

    public void SendTrovoMessage(string message, bool isWhisper)
    {
        CPH.SendTrovoMessage(message, isWhisper);
    }

    public void SetGlobalVariable<T>(string name, T value, bool persist)
    {
        CPH.SetGlobalVar(name, value, persist);
    }

    public T GetGlobalVariable<T>(string name, bool persist)
    {
        return CPH.GetGlobalVar<T>(name, persist);
    }

    private string CleanMessageInput(string messageInput)
    {
        messageInput = messageInput.Replace("\"", "\\\"");
        if (string.IsNullOrWhiteSpace(messageInput))
        {
            messageInput = "send a snarky comment about how rude it is to interrupt somebody, or say their name without asking a question";
        }

        return messageInput;
    }
}

public class ChatGptApiRequest
{
    private readonly string _apiKey;
    private readonly string _endpoint = "https://api.openai.com/v1/chat/completions";
    private readonly List<string> _chatHistory;
    private readonly CPHInline _cphInline;
    public ChatGptApiRequest(string apiKey, List<string> chatHistory, CPHInline cphInline)
    {
        _apiKey = apiKey;
        _chatHistory = chatHistory;
        _cphInline = cphInline;
    }

    public string GenerateResponse(string prompt, string gptModel, string content, double gptTempValue)
    {
        string chatContext = String.Join("\n", _chatHistory);
        string contextWithPrompt = $"{chatContext}\n\nUser: {prompt}";
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_endpoint);
        request.Headers.Add("Authorization", "Bearer " + _apiKey);
        request.ContentType = "application/json";
        request.Method = "POST";
        // Build JSON object
        var payload = new
        {
            model = gptModel,
            max_tokens = 100,
            temperature = gptTempValue,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = content
                },
                new
                {
                    role = "user",
                    content = contextWithPrompt
                }
            }
        };
        // Convert object to JSON
        string requestBody = JsonConvert.SerializeObject(payload);
        _cphInline.LogInfo("Request Body: " + requestBody);
        byte[] bytes = Encoding.UTF8.GetBytes(requestBody);
        request.ContentLength = bytes.Length;
        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(bytes, 0, bytes.Length);
        }

        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        catch (WebException ex)
        {
            using (HttpWebResponse response = (HttpWebResponse)ex.Response)
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
            {
                string errorResponse = reader.ReadToEnd();
                _cphInline.LogError("Error Response from API: " + errorResponse);
            }

            throw; // Re-throw the exception to be handled elsewhere
        }
    }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}

public class Choice
{
    public Message message { get; set; }
    public int index {get; set; }
    public object logprobs { get; set; }
    public string finish_reason { get; set; }
}

public class Root
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<Choice> choices { get; set; }
    public Usage usage { get; set; }
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
}
// Mustached Maniac code reworked by EmptyProfile
// http://www.twitch.tv/emptyprofile
