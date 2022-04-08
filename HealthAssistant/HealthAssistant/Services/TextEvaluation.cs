using System.Globalization;
using System.Text.RegularExpressions;

namespace HealthAssistant.Services
{
    /// <summary>
    /// Enum of commands or intent that the evaluator recognizes
    /// </summary>
    public enum SpeechCommand
    {
        Undefined,
        Show,
        ShowBloodPressure,
        ShowGlucose,
        ShowPulse,
        ShowTemperature,
        ShowWeight,
        Input,
        InputBloodPressure,
        InputGlucose,
        InputPulse,
        InputTemperature,
        InputWeight,
        Pause,
        Continue,
        ClearInput,
        Help
    }

    // TODO
    // Currently the strings for two languages are defiend in the search arrays. But this needs to be separated for the differnet languages
    // to avoid ambiguity

    /// <summary>
    /// Class to evaulate a given string and provides methods to extract the intent and data
    /// Regular expression or search for fixed values is used
    /// New commands must be added in teh enumeration and the evaluation for the string that trigger the command must be handled in
    /// the method GetCommandInText.
    /// </summary>
    public class TextEvaluation
    {
        /// <summary>
        /// Try to find a string that matches a command
        /// First we try to find the desired acitvity in a secodn step more information is searched
        /// For exampel searcg find strings for the command SpeechCommand.Show
        /// In a second step further evaluation is made to find which Measurement should be shown
        /// </summary>
        /// <param name="Text">String to be evaluated</param>
        /// <returns>An Enum from SpeechCommand</returns>
        public SpeechCommand GetCommandInText(string Text)
        {
            if (Text == null)
                return SpeechCommand.Undefined;
            SpeechCommand actvivity = SpeechCommand.Undefined;
            string evalString = Text.ToLower();

            string[] exitCommandArray = { "continue", "go on", "weiter" };
            if (exitCommandArray.Any(evalString.Contains))
            {
                return SpeechCommand.Continue;
            }

            string[] pauseCommandArray = { "pause", "break", "warte" };
            if (pauseCommandArray.Any(evalString.Contains))
            {
                return SpeechCommand.Pause;
            }

            string[] helpCommandArray = { "help", "hilfe" };
            if (pauseCommandArray.Any(evalString.Contains))
            {
                return SpeechCommand.Help;
            }

            string[] resetCommandArray = { "clear", "stop", "delete", "new", "restart", "löschen", "neu" };
            if (pauseCommandArray.Any(evalString.Contains))
            {
                return SpeechCommand.ClearInput;
            }

            string[] listArray = { "list", "table", "chart", "graph", "show", "grafik", "listen", "liste", "tabelle", "zeige" };
            if (listArray.Any(evalString.Contains))
            {
                actvivity = SpeechCommand.Show;
            }
            string[] inputArray = { "input", "enter", "put in", "eingabe" };
            if (inputArray.Any(evalString.Contains))
            {
                actvivity = SpeechCommand.Input;
            }

            // No check for more detials in the given string to get a complete intent
            // if no activity is given the fall back is always the input data intent
            string[] bloodPressureArray = { "pressure", "dia", "sys", "blutdruck" };
            if (bloodPressureArray.Any(evalString.Contains))
            {
                switch (actvivity)
                {
                    case SpeechCommand.Input:
                        return SpeechCommand.InputBloodPressure;

                    case SpeechCommand.Show:
                        return SpeechCommand.ShowBloodPressure;

                    default:
                        return SpeechCommand.InputBloodPressure;
                }
            }
            string[] pulsArray = { "heart reate", "beats per minute", "bpm", "puls" };
            if (pulsArray.Any(evalString.Contains))
            {
                switch (actvivity)
                {
                    case SpeechCommand.Input:
                        return SpeechCommand.InputPulse;

                    case SpeechCommand.Show:
                        return SpeechCommand.ShowPulse;

                    default:
                        return SpeechCommand.InputPulse;
                }
            }
            string[] tempArray = { "temp", "degree" };
            if (tempArray.Any(evalString.Contains))
            {
                switch (actvivity)
                {
                    case SpeechCommand.Input:
                        return SpeechCommand.InputTemperature;

                    case SpeechCommand.Show:
                        return SpeechCommand.ShowTemperature;

                    default:
                        return SpeechCommand.InputTemperature;
                }
            }
            string[] weightArray = { "weight", "weighing", "gewicht", "wiege" };
            if (weightArray.Any(evalString.Contains))
            {
                switch (actvivity)
                {
                    case SpeechCommand.Input:
                        return SpeechCommand.InputWeight;

                    case SpeechCommand.Show:
                        return SpeechCommand.ShowWeight;

                    default:
                        return SpeechCommand.InputWeight;
                }
            }
            string[] glucoseArray = { "glucose", "sugar", "zucker" };
            if (glucoseArray.Any(evalString.Contains))
            {
                switch (actvivity)
                {
                    case SpeechCommand.Input:
                        return SpeechCommand.InputGlucose;

                    case SpeechCommand.Show:
                        return SpeechCommand.ShowGlucose;

                    default:
                        return SpeechCommand.InputGlucose;
                }
            }
            // necessary too avoid compiler error CS0161 (not all path return a value)
            if (actvivity != SpeechCommand.Undefined)
            {
                return actvivity;
            }
            else
            {
                return SpeechCommand.Undefined;
            }
        }

        /// <summary>
        /// Try to find a Date in the given stringstring that matches a command
        /// </summary>
        /// <param name="Text">String to be evaluated</param>
        /// <returns>
        /// Null if no Date could be found.
        /// The returned DateTime may contain a time portion.
        /// </returns>
        public DateTime? GetDateValue(string Text)
        {
            if (Text == null)
                return null;

            string evalString = Text.ToLower();
            string[] inputArray = { "now", "this moment", "jetzt", "gerade" };
            if (inputArray.Any(evalString.Contains))
            {
                return DateTime.Now;
            }
            string[] dayInputArray = { "today", "this day", "this morning", "this evening", "heute" };
            if (dayInputArray.Any(evalString.Contains))
            {
                return DateTime.Now.Date;
            }
            string[] dateInputArray = { "yesterday", "gestern" };
            if (dateInputArray.Any(evalString.Contains))
            {
                return DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
            }
            string pattern = @"\d{1,2}.\d{1,2}.\d{1,4}";  //1.12.2022
            var matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var dateMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var day = Convert.ToInt32(dateMatches[0].Value);
                var month = Convert.ToInt32(dateMatches[1].Value);
                var year = Convert.ToInt32(dateMatches[2].Value);
                if (dateMatches.Count == 4)
                {
                    year = year * 100 + Convert.ToInt32(dateMatches[3].Value);
                }
                DateTime dt = new DateTime(year, month, day);
                return dt;
            }

            // German month
            string[] germanMonthArray = { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" };
            string[] americnaMonthArray = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "Dezember" };
            pattern = @"\d{1,2}. (?:Januar|Februar|März|April|Mai|Juni|Juli|August|September|Oktober|November|Dezember|January|February|March|May|June|July|October|December) \d{1,4}"; // 1. Januar 2022

            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var dateMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var day = Convert.ToInt32(dateMatches[0].Value);
                var year = Convert.ToInt32(dateMatches[1].Value);
                if (dateMatches.Count == 3)
                {
                    year = year * 100 + Convert.ToInt32(dateMatches[2].Value);
                }
                pattern = @"(?:Januar|Februar|März|April|Mai|Juni|Juli|August|September|Oktober|November|Dezember)";
                var monthMatch = Regex.Matches(matches[0].Value, pattern);
                if (monthMatch.Count > 0)
                {
                    var month = Array.IndexOf(germanMonthArray, monthMatch[0].Value);
                    DateTime dt = new DateTime(year, month + 1, day);
                    return dt;
                }
                else
                {
                    // try the American month
                    pattern = @"(?:January|February|March|April|May|June|July|September|October|November|December)";
                    var americanMonthMatch = Regex.Matches(matches[0].Value, pattern);
                    var month = Array.IndexOf(americnaMonthArray, americanMonthMatch[0].Value);
                    DateTime dt = new DateTime(year, month + 1, day);
                    return dt;
                }
            }

            return null;
        }

        /// <summary>
        /// Try to find a value in the given string
        /// </summary>
        /// <param name="Text">String to be evaluated</param>
        /// <returns></returns>
        public double? GetFirstValue(string Text)
        {
            if (Text == null)
                return null;
            string matchValue = "";
            string pattern = @"(?:um|at) \d{2,3}(?:.|,)?\d{0,1}(?: |$)";
            var matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            int timePatternStart = 0;
            int timePatternEnd = 0;
            if (matches.Count > 0)
            {
                timePatternStart = matches[0].Index;
                timePatternEnd = matches[0].Index + matches[0].Length;
            }
            pattern = @"(?: |)\d{2,3}(?:.|,)?\d{0,1}(?: |$)";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            foreach (Match item in matches)
            {
                if ((item.Index < timePatternStart) || (item.Index > timePatternEnd) || (timePatternStart == timePatternEnd))
                {
                    matchValue = item.Value;
                }
            }
            if (String.IsNullOrEmpty(matchValue))
            {
                return null;
            }
            else
            {
                NumberFormatInfo formatProvider = new NumberFormatInfo();
                if (matchValue.Contains('.'))
                {
                    formatProvider.NumberDecimalSeparator = ".";
                }
                else
                {
                    formatProvider.NumberDecimalSeparator = ",";
                }
                return Convert.ToDouble(matchValue, formatProvider);
            }
        }

        /// <summary>
        /// Try to find a second value in the string. This is necessary if the two blood pressure values are given together
        /// </summary>
        /// <param name="Text">String to be evaluated</param>
        /// <returns></returns>
        public double? GetSecondValue(string Text)
        {
            if (Text == null)
                return null;

            string pattern = @"(?:um|at) \d{2,3}(?:.|,)?\d{0,1}(?: |$)";
            var matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            int timePatternStart = 0;
            int timePatternEnd = 0;
            if (matches.Count > 0)
            {
                timePatternStart = matches[0].Index;
                timePatternEnd = matches[0].Index + matches[0].Length;
            }
            pattern = @" \d{2,3}(?:.|,)?\d{0,1}(?: |$)";

            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            bool firstFound = false;
            foreach (Match item in matches)
            {
                if ((item.Index < timePatternStart) || (item.Index > timePatternEnd) || (timePatternStart == timePatternEnd))
                {
                    if (firstFound)
                    {
                        return Convert.ToDouble(item.Value);
                    }
                    else
                    {
                        firstFound = true;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Try to extract a Time from the input.
        /// </summary>
        /// <param name="Text">String to be evaluated</param>
        /// <returns>
        /// Null if no Time portion could be found.
        /// The returned DateTime may contain a date value.
        /// If only time was found the Date part is set to DateTime.MinValue
        /// </returns>
        public DateTime? GetTime(string Text)
        {
            if (Text == null)
                return null;

            bool isPMTime = false;
            string evalString = Text.ToLower();
            string[] inputArray = { "now", "this moment", "jetzt", "gerade" };
            if (inputArray.Any(evalString.Contains))
            {
                return DateTime.Now;
            }

            string pattern = @"\d{1,2} PM";
            var matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                isPMTime = true;
            }

            pattern = @"(?:\d{1,2} minutes ago|vor \d{1,2} minuten)";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var minuteMatch = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var minute = Convert.ToInt32(minuteMatch[0].Value);
                return DateTime.Now.AddMinutes(-minute);
            }

            pattern = @"\d{1,2}:{1}\d\d";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                if (TimeSpan.TryParse(matches[0].Value, out var time))
                {
                    return DateTime.MinValue + Get24HourTime(isPMTime, time);
                }
            }

            pattern = @"\d{1,2} (?:vor|to) {1}\d{1,2}";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var timeMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var hour = Convert.ToInt32(timeMatches[1].Value);
                var minute = 60 - Convert.ToInt32(timeMatches[1].Value);
                if ((minute <= 30) && (minute > 0) && (hour <= 24) && (hour > 0))
                {
                    return DateTime.MinValue + Get24HourTime(isPMTime, new TimeSpan(hour, minute, 0));
                }
            }
            pattern = @"\d{1,2} (?:nach|past) {1}\d{1,2}";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var timeMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var hour = Convert.ToInt32(timeMatches[0].Value);
                var minute = Convert.ToInt32(timeMatches[1].Value);
                if ((minute <= 30) && (minute > 0) && (hour <= 24) && (hour > 0))
                {
                    return DateTime.MinValue + Get24HourTime(isPMTime, new TimeSpan(hour, minute, 0));
                }
            }

            pattern = @"(?:um |at |)\d{1,2}";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var timeMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var hour = Convert.ToInt32(timeMatches[0].Value);
                if ((hour <= 24) && (hour > 0))
                {
                    return DateTime.MinValue + Get24HourTime(isPMTime, new TimeSpan(hour, 0, 0));
                }
            }

            pattern = @"(?:quarter|viertel)(?: vor | to )\d{1,2}";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var timeMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var hour = Convert.ToInt32(timeMatches[0].Value);
                return DateTime.MinValue + Get24HourTime(isPMTime, new TimeSpan(hour - 1, 45, 0));
            }

            pattern = @"(?:quarter|viertel)(?: nach | past )\d{1,2}";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var timeMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var hour = Convert.ToInt32(timeMatches[0].Value);
                return DateTime.MinValue + Get24HourTime(isPMTime, new TimeSpan(hour, 15, 0));
            }

            pattern = @"(?:half past )\d{1,2}";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var timeMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var hour = Convert.ToInt32(timeMatches[0].Value);
                return DateTime.MinValue + Get24HourTime(isPMTime, new TimeSpan(hour, 30, 0));
            }

            pattern = @"(?:halb )\d{1,2}";
            matches = Regex.Matches(Text, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var timeMatches = Regex.Matches(matches[0].Value, @"\d{1,2}");
                var hour = Convert.ToInt32(timeMatches[0].Value);
                return DateTime.MinValue + Get24HourTime(isPMTime, new TimeSpan(hour - 1, 30, 0));
            }

            return null;
        }

        /// <summary>
        /// This method is used to analyze answers to confirmations
        /// </summary>
        /// <param name="Text">String to be evaluated</param>
        /// <returns></returns>
        public bool IsConfirmationText(string Text)
        {
            string evalString = Text.ToLower();
            string[] exitCommandArray = { "continue", "go on", "yes", "of course", "do it", "weiter", "ja", "sicher", "ok", "yep" };
            if (exitCommandArray.Any(evalString.Contains))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Calculate the 24 hours TimeSpan from a TimeSpan from a tiem with PM
        /// </summary>
        /// <param name="IsPM">Flag to indicate that the time is PM </param>
        /// <param name="Time">Originla TimeSpan </param>
        /// <returns></returns>
        private TimeSpan Get24HourTime(bool IsPM, TimeSpan Time)
        {
            if (IsPM)
            {
                return Time.Add(new TimeSpan(12, 0, 0));
            }
            else
            {
                return Time;
            }
        }
    }
}