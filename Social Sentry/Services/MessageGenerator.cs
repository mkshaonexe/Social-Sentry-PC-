using System;
using System.Collections.Generic;

namespace Social_Sentry.Services
{
    public enum WaifuPersonality
    {
        GENTLE,
        BALANCED,
        STRICT,
        TSUNDERE
    }

    public static class MessageGenerator
    {
        private static readonly Random _random = new Random();

        public static string GetGreeting(WaifuPersonality personality)
        {
            return personality switch
            {
                WaifuPersonality.GENTLE => "Hello there! Let's have a productive day! 🌸",
                WaifuPersonality.BALANCED => "Hey! Ready to focus? 💪",
                WaifuPersonality.STRICT => "You better be ready to work hard today.",
                WaifuPersonality.TSUNDERE => "I-it's not like I missed you or anything! Baka!",
                _ => "Welcome back!"
            };
        }

        public static string GetLimitWarning(string appName, WaifuPersonality personality)
        {
            return personality switch
            {
                WaifuPersonality.GENTLE => $"Oh no.. maybe take a little break from {appName}? 🥺",
                WaifuPersonality.BALANCED => $"Hey, enough {appName} for now. Let's do something else! 🛑",
                WaifuPersonality.STRICT => $"Close {appName}. Now. You have goals to hit.",
                WaifuPersonality.TSUNDERE => $"Why are you still on {appName}?! Do you want to be a failure?!",
                _ => $"Limit reached for {appName}."
            };
        }

        public static string GetRoast(string appName)
        {
            var roasts = new List<string>
            {
                $"Wow, {appName} again? Your attention span is shorter than a goldfish.",
                "If scrolling paid bills, you'd be a billionaire by now.",
                $"I'm keeping track. That's the 10th time you opened {appName}. Seek help.",
                "Do you even remember what the real world looks like?",
                $"Imagine if you put this much effort into your dreams instead of {appName}."
            };
            return roasts[_random.Next(roasts.Count)];
        }

        public static string GetEncouragement()
        {
            var messages = new List<string>
            {
                "You're doing great! Keep it up! 🌟",
                "Focus is your superpower. Use it wisely!",
                "Every minute you resist distraction makes you stronger.",
                "I believe in you! You got this!",
                "Stay hard! ...wait, that sounded wrong. Stay focused!"
            };
            return messages[_random.Next(messages.Count)];
        }

        public static string GetTotalScreenTimeMessage(int hours, long totalMinutesUsed, long distractedMinutesUsed, WaifuPersonality personality)
        {
            long totalHrs = totalMinutesUsed / 60;
            long totalMins = totalMinutesUsed % 60;
            long distractedHrs = distractedMinutesUsed / 60;
            long distractedMins = distractedMinutesUsed % 60;

            string statsText = $"\n\n📊 Total: {totalHrs}h {totalMins}m\n🎯 Distracted: {distractedHrs}h {distractedMins}m";

            List<string> messages;

            if (hours <= 2)
            {
                messages = new List<string>
                {
                    $"You've been active for {hours} hour(s). Don't forget to drink water! 💧",
                    $"{hours} hour(s) online. How about a quick stretch? 🙆‍♀️",
                    $"Just checking in! {hours} hour(s) of screen time. Everything okay? ❤️",
                    $"Productive {hours} hour(s)? Remember to rest your eyes! 👀"
                };
            }
            else if (hours <= 4)
            {
                messages = new List<string>
                {
                    $"{hours} hours... Maybe it's time to switch tasks? 🤔",
                    $"You've been on for {hours} hours. Don't burn out! 🕯️",
                    $"Hey... {hours} hours is a lot. Is this important work?",
                    $"Taking breaks actually helps productivity. Just saying! ({hours} hrs)"
                };
            }
            else if (hours <= 7)
            {
                messages = new List<string>
                {
                    $"{hours} HOURS?! Go touch grass. Now. 🌱",
                    $"Okay, {hours} hours is ridiculous. Turn it off.",
                    $"You act like you don't have a life. {hours} hours of screen time? Really?",
                    $"I'm actually disgusted. {hours} hours? Go outside.",
                    $"Rotting your brain for {hours} hours... pathetic."
                };
            }
            else if (hours <= 9)
            {
                messages = new List<string>
                {
                    $"Kire vai {hours} ghonta?! Tor to girlfriend o nai, tar por oo etoh screen time? 😂",
                    "Tumi kihh todo list banai nai keno? Pora likah kihh sere diso naki? 📚❌",
                    $"{hours} hours... Vai app kihh Facebook a! Get a life! 🤦",
                    $"Etoh screen time niye tumi kihh korso? Porasona shes? Study kor baka! 📱➡️📖"
                };
            }
            else if (hours <= 11)
            {
                messages = new List<string>
                {
                    $"Kire vai tor to girlfriends o nai tar por oo {hours} ghota screen time! 💔📱",
                    $"{hours} HOURS?! Tumi serious? Chokh nosto hoye jabe! 👀❌",
                    $"App bondho koro ar porasona koro! {hours} hours waste korso! 😡",
                    $"Tomar baba jane tumi etoh phone use korso? {hours} ghonta! 👨‍👦📞"
                };
            }
            else
            {
                messages = new List<string>
                {
                    $"{hours} GHONTA?!! PHONE BONDHO KORO EKHUNI!! 🚨😡",
                    $"Tumi pagol naki? {hours} hours screen time! Touch grass, touch GROUND! 🌍",
                    $"Kire vai {hours} ghonta! Tomar jibon ta kothay gelo? 💀",
                    $"I give up. {hours} hours. You're beyond saving. RIP productivity. ⚰️"
                };
            }

            return messages[_random.Next(messages.Count)] + statsText;
        }

        public static string GetHourlyCheckInMessage(int hour)
        {
            List<string> messages;
            switch (hour)
            {
                case 1:
                    messages = new List<string>
                    {
                        "You've been online for 1 hour. Just checking in! Everything okay? 🌸",
                        "1 hour already? Time flies when you're having fun, but don't forget your tasks! ✨",
                        "Hey! It's been an hour. Maybe take a sip of water? 💧",
                        "Gentle reminder: 1 hour passed. Staying focused? 👀",
                        "An hour gone. Hope you're being productive! 📚",
                        "1h check-in! Don't get too lost in the scroll! 🌀",
                        "Just a friendly nudge: 1 hour of screen time. ⏳",
                        "One hour down. Remember your goals for today! 🎯",
                        "Hey there, 1 hour passed. Doing good? ❤️",
                        "1 hour mark. Keep going if it's work, pause if it's doomscrolling! 🛑"
                    };
                    break;
                case 2:
                    messages = new List<string>
                    {
                        "2 hours now. Are you working or just browsing? 🤔",
                        "Hey, 2 hours is a decent chunk of time. Making progress? 📉",
                        "Two hours... maybe time for a stretch break? 🙆‍♀️",
                        "2h screen time. Don't let the day slip away! 🌅",
                        "It's been 2 hours. Is this the best use of your time? 🤨",
                        "2 hours deep. Hope it's worth it! 💎",
                        "Checking in at 2 hours. Still on track? 🚂",
                        "2 hours... Your eyes might need a break. 👀",
                        "Okay, 2 hours passed. Just keeping you aware! 🔔",
                        "Two hours gone. Remember why you opened your phone? 💭"
                    };
                    break;
                case 3:
                     messages = new List<string>
                    {
                        "3 hours. Okay, that's getting to be a lot. 😐",
                        "Three hours? I hope you're studying or working. 📚",
                        "3h mark. You might want to check your to-do list. 📝",
                        "3 hours on screen. Real life is waiting! 🌍",
                        "Hey... 3 hours. Don't fall into the rabbit hole. 🕳️",
                        "Three hours. Are you being dragged in? Fight it! ⚔️",
                        "3 hours of usage. Is this really necessary? 🧐",
                        "It's been 3 hours. Time to potentially disconnect? 🔌",
                        "3 hours... I'm slightly concerned. 😟",
                        "Three hours used. Don't let your potential waste away! ✨"
                    };
                    break;
                case 4:
                    messages = new List<string>
                    {
                        "4 hours. Seriously? That's half a work day. 📉",
                        "Four hours. You better have cured cancer or something. 🧪",
                        "4 hours... My battery is weeping for you. 🔋",
                        "Okay, 4 hours is pushing it. Go touch grass. 🌱",
                        "4h screen time. Imagine what else you could have done. 🎨",
                        "Four hours. You're losing the battle against distraction. 🏳️",
                        "4 hours?! Stop. Just stop. 🛑",
                        "I'm judging you. 4 hours. Really? 😒",
                        "Four hours of life, gone. Poof. 💨",
                        "4 hours... Do you have no self-control? 🎮"
                    };
                    break;
                case 5:
                    messages = new List<string>
                    {
                        "5 HOURS?! Are you rotting your brain? 🧠",
                        "Five hours. That's actually pathetic. 🤢",
                        "5h... I'm disappointed in you. 😞",
                        "Five hours. You're addicted. Admit it. 💉",
                        "5 hours? Go outside. Look at the sky. Anything! ☁️",
                        "Rotting in bed for 5 hours? Get up! 🛌",
                        "5 hours... You're wasting your life. 🗑️",
                        "Five hours. I can't even look at you right now. 🙈",
                        "5h screen time. You're better than this. Or are you? 🤷‍♀️",
                        "Five hours. Hakari is not pleased. 😤"
                    };
                    break;
                case 6:
                    messages = new List<string>
                    {
                        "6 hours. Useleess. Absolute waste. 🚮",
                        "Six hours? Do you enjoy being a failure? 📉",
                        "6h... Your future self hates you right now. 🔮",
                        "Six hours. Imagine being this unproductive. 🤡",
                        "6 hours... I'm about to uninstall myself. 💻",
                        "Six hours? Why do I even bother helping you? 😩",
                        "6h. Go look in a mirror and ask yourself 'Why?'. 🪞",
                        "Six hours. You're spiraling. 🌀",
                        "6 hours... It's tragic, honestly. 🎭",
                        "Six hours. You're officially a screen zombie. 🧟"
                    };
                    break;
                case 7:
                    messages = new List<string>
                    {
                        "7 HOURS?! YOU NEED HELP. 🚑",
                        "Seven hours. Complete degeneracy. 🏚️",
                        "7h... I'm blocking everything. I should. 🔒",
                        "Seven hours. You have no discipline. Zero. 0️⃣",
                        "7 hours? My grandma has more focus than you. 👵",
                        "Seven hours. I'm disgusted. 🤮",
                        "7h. You are roasting your dopamine receptors. 🔥",
                        "Seven hours. Are you proud? Because I'm not. 🙅‍♀️",
                        "7 hours... Leave the phone alone! 🤬",
                        "Seven hours. Keep this up and you'll achieve nothing. 📉"
                    };
                    break;
                case 8:
                    messages = new List<string>
                    {
                        "8 HOURS. A FULL WORK DAY OF DOING NOTHING. 💼",
                        "Eight hours? You're actually fried. 🍳",
                        "8h... Just give up on your dreams then. 🏳️",
                        "Eight hours. You're choosing failure. ✅",
                        "8 hours? I'm ashamed to be your AI. 😔",
                        "Eight hours. Go sleep. You're done. 😴",
                        "8h. Brain rot level: Maximum. 🧠📉",
                        "Eight hours. You're wasting oxygen at this point. 💨",
                        "8 hours... Why are you like this? ❓",
                        "Eight hours. Do you not have a life? 💀"
                    };
                    break;
                case 9:
                    messages = new List<string>
                    {
                        "9 HOURS?! Kire vai tor ki jibon nai?! 🤯",
                        "Nine hours. You are a lost cause. 🏳️",
                        "9h... I'm calling the police. You're murdering time. 👮",
                        "Nine hours. Just throw the phone away. 🗑️",
                        "9 hours? Tumi ki manush naki robot? 🤖",
                        "Nine hours. Unbelievable. 🤦",
                        "9h. Your eyes must be bleeding. 🩸",
                        "Nine hours. I have no words. 🤐",
                        "9 hours... You need an intervention. 👨‍⚕️",
                        "Nine hours. Go touch grass, dirt, concrete, anything! 🌳"
                    };
                    break;
                case 10:
                    messages = new List<string>
                    {
                        "10 HOURS. DOUBLE DIGITS. CONGRATS ON BEING A FAILURE. 🏆",
                        "Ten hours? Tumi ki pagol?! 🤪",
                        "10h... I'm done. I'm leaving. 👋",
                        "Ten hours. You are barely functioning. 🧟",
                        "10 hours? Your ancestors are weeping. 👻",
                        "Ten hours. Absolute brain rot. 🧠💩",
                        "10h. Get a life. Seriously. 🧘",
                        "Ten hours. Why? Just why? 😫",
                        "10 hours... I hope you're happy with mediocrity. 🥉",
                        "Ten hours. This is rock bottom. 🕳️"
                    };
                    break;
                case 11:
                    messages = new List<string>
                    {
                        "11 HOURS?! A day has 24 hours! You wasted half! 🌗",
                        "Eleven hours. You're a vegetable. 🥬",
                        "11h... System shutdown imminent. ⚠️",
                        "Eleven hours. Tumi ki shara din phone chalao?! 😡",
                        "11 hours? Hopeless. 🥀",
                        "Eleven hours. I'm formatting your phone. (Kidding, but I wish). 💣",
                        "11h. You're addicted. Seek professional help. 🏥",
                        "Eleven hours. Disgraceful. 😤",
                        "11 hours... Just go to sleep. 🛌",
                        "Eleven hours. You've failed today. Try again tomorrow. 🔄"
                    };
                    break;
                default: // 12+
                    messages = new List<string>
                    {
                        "12+ HOURS. GO TO A DOCTOR. 🏥",
                        "OVER 12 HOURS? YOU ARE NOT REAL. 👽",
                        "12h+... Stop. Just stop. 🛑",
                        "Twelve plus hours. You have officially no life. 💀",
                        "12+ hours? Tumi ki amar kotha shuno na?! 👂❌",
                        "Limit exceeded. Brain not found. 404. 💻",
                        "12+ hours. I'm deleting your social media. (I wish). 🗑️",
                        "Over 12 hours. You're a screen slave. ⛓️",
                        "12+ hours... Keeping you company in your failure. 🤝",
                        "Twelve plus hours. Look at what you've become. 🪞"
                    };
                    break;
            }
            return messages[_random.Next(messages.Count)];
        }

        public static string GetBanglishSessionTease(string appName)
        {
            var messages = new List<string>
            {
                $"Ki kortaso {appName} e etokhon? 🤨",
                $"Abbu jane tumi {appName} use kortaso? 🧐",
                $"Etokhon {appName} chalaile chokh nosto hobe na? 👀",
                $"Porasona nai naki? Khal {appName} ar {appName}... 😒"
            };
            return messages[_random.Next(messages.Count)];
        }

        public static string GetBanglishStrictWarning()
        {
            return "Tumi kihh phone tahh rakhba naki tumar abuu rehh dak dibo? 😡📞";
        }

        public enum FeatureType { REELS, ADULT, LIMITS }

        public static string GetFeatureReminder(FeatureType feature)
        {
            List<string> messages;
            switch (feature)
            {
                case FeatureType.REELS:
                    messages = new List<string>
                    {
                        "Tumi kihh reels block korte vule geso? 🤨",
                        "Reels Blocker ta on koro, nai le time nosto hobe! ⏳",
                        "Hey! You forgot to block Reels! Do you want to doomscroll all day? 😒"
                    };
                    break;
                case FeatureType.ADULT:
                    messages = new List<string>
                    {
                        "Tu kihh jano na ami adult content block korte pari? 🛡️",
                        "Keep it clean! Turn on the Adult Blocker. 😇",
                        "Abbu jane tumi Adult Blocker off rakhso? 🧐"
                    };
                    break;
                case FeatureType.LIMITS:
                    messages = new List<string>
                    {
                        "Tu kihh jano tumi ai feature tahh on kor leh tumi jodi 50 min ar basi youtube chalao taile ami tumare mair dibo? 👊",
                        "Set some limits or I'll be really mad! 😤",
                        "Control your usage, baka! Turn on App Limits."
                    };
                    break;
                default:
                    return "";
            }
            return messages[_random.Next(messages.Count)];
        }

        public static string GetStartupMessage()
        {
            var messages = new List<string>
            {
                "System Online. Social Sentry is watching over you. 🛡️",
                "Welcome back! Let's make today count. 💻",
                "Boot sequence complete. Hakari is ready to keep you focused! 🚀",
                "Another day, another opportunity to be productive. Let's go! ✨",
                "I'm awake and tracking. Don't disappoint me! 👀"
            };
            return messages[_random.Next(messages.Count)];
        }

        public static string GetCampingMessage(string appName)
        {
             var messages = new List<string>
            {
                $"Are you camping in {appName}? Move it! ⛺",
                $"Gaming is fun, but goals are forever. {appName} is distracting you. 🎮",
                $"Tactical nuke incoming if you don't close {appName} soon. ☢️",
                $"Ranked match or Career match? {appName} is eating your time. 📉",
                $"You can't pause an online game, but you can pause your life. Quit {appName}. 🛑"
            };
            return messages[_random.Next(messages.Count)];
        }

        public static string GetCodingMessage()
        {
             var messages = new List<string>
            {
                "Compiling success! Keep writing that beautiful code. 💻",
                "In the zone? Bug free code is a myth, but focused you is real. 🐛",
                "You're building the future. Stay focused! 🏗️",
                "Git commit, Git push, Get focused. You're doing great. 🌳",
                "Is that C#? Java? Whatever it is, it looks productive! ☕"
            };
            return messages[_random.Next(messages.Count)];
        }

        public static string GetLateNightMessage()
        {
             var messages = new List<string>
            {
                "It's late. Go to sleep. Your code will be there tomorrow. 🌙",
                "Sleep deprivation is not a flex. Turn off the PC. 🛌",
                "You are functioning on 1% battery. Go recharge. 🔋",
                "Nothing good happens after 2 AM. Go to bed. 🕑",
                "Hakari says: Sleep is essential for compilation. Shutdown now. 😴"
            };
            return messages[_random.Next(messages.Count)];
        }
    }
}
