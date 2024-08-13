using System;
using System.Collections.Generic;

public class CPHInline
{
    public bool Execute()
    {
        // Retrieve the message and username
        string message = args["rawInput"].ToString();
        string user = args["userName"].ToString();

        // Get the current time when the message is logged
        string currentTime = DateTime.Now.ToString("HH:mm:ss");

        // Combine the current time with the message
        string combinedMessage = $"{currentTime} - {user}: {message}";

        // Retrieve chat history or initialize a new list
        List<string> chatHistory = CPH.GetGlobalVar<List<string>>("chatHistory", false) ?? new List<string>();

        // Add the new message to the end of the list
        chatHistory.Add(combinedMessage);

        // Retrieve the maximum number of messages allowed from chatGptMessageCount
        int maxMessages = CPH.GetGlobalVar<int>("chatGptMessageCount");

        // Ensure the list does not exceed the maximum number of messages
        if (chatHistory.Count > maxMessages)
        {
            chatHistory.RemoveAt(0);
        }

        // Update the global variable with the modified chat history
        CPH.SetGlobalVar("chatHistory", chatHistory, false);

        return true;
    }
}

// Code by EmptyProfile
// http://www.twitch.tv/emptyprofile
