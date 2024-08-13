using System;
using System.Collections.Generic;

public class CPHInline
{
    public bool Execute()
    {
        string apiKey = args["chatGptApiKey"].ToString();
        string gptBehavior = args["behavior"].ToString();
        string gptModel = args["chatGptModel"].ToString();
        string gptReplyProb = args["chatGptReplyProb"].ToString();
        bool broadcasterReplies = Convert.ToBoolean(args["broadcasterReplies"]);
        int messageCount = Convert.ToInt32(args["chatGptMessageCount"]);
        string model;
        if (gptModel == "3.5")
        {
            model = "gpt-3.5";
        }
        else if (gptModel == "4")
        {
            model = "gpt-4";
        }
        else if (gptModel.ToLower() == "4t")
        {
            model = "gpt-4-turbo";
        }
        else if (gptModel.ToLower() == "4m")
        {
            model = "gpt-4o-mini";
        }
        else
        {
            model = "gpt-4o-mini";
        }

        List<string> excludeGptReply = new List<string>
        {
            "streamelements",
            "nightbot",
            "streamlabs",
            "pokemoncommunitygame",
            "kofistreambot",
            "fourthwallhq"
        };

        var twitchBot = CPH.TwitchGetBot();
        if (twitchBot != null && !string.IsNullOrEmpty(twitchBot.UserLogin))
        {
            excludeGptReply.Add(twitchBot.UserLogin.ToLower());
        }

        var twitchBroadcaster = CPH.TwitchGetBroadcaster();
        if (twitchBroadcaster != null && !string.IsNullOrEmpty(twitchBroadcaster.UserLogin))
        {
            if (!broadcasterReplies)
            {
                excludeGptReply.Add(twitchBroadcaster.UserLogin.ToLower());
            }
        }

        CPH.SetGlobalVar("chatGptReplyProb", gptReplyProb, true);
        CPH.SetGlobalVar("chatGptExclusions", excludeGptReply, true);
        CPH.SetGlobalVar("chatGptModel", model, true);
        CPH.SetGlobalVar("chatGptApiKey", apiKey, true);
        CPH.SetGlobalVar("chatGptBehavior", gptBehavior, true);
        CPH.SetGlobalVar("chatGptMessageCount", messageCount, true);  // New variable
        return true;
    }
}

// Mustached Maniac code reworked by EmptyProfile
// http://www.twitch.tv/emptyprofile
