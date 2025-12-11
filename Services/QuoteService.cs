using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DeltaForceTracker.Services
{
    public class QuoteService : IDisposable
    {
        private List<string> _quotes;
        private Random _random;
        private List<string> _remainingQuotes;

        public QuoteService()
        {
            _random = new Random();
            _quotes = new List<string>();
            _remainingQuotes = new List<string>();
            LoadQuotes();
        }

        private void LoadQuotes()
        {
            try
            {
                // Try to load from Resources/Data/affirmations.json
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "affirmations.json");
                
                if (File.Exists(jsonPath))
                {
                    var json = File.ReadAllText(jsonPath);
                    var data = JsonConvert.DeserializeObject<QuoteData>(json);
                    _quotes = data?.Quotes ?? new List<string>();
                }
                else
                {
                    // Fallback: load from embedded resource or provide default quotes
                    _quotes = GetDefaultQuotes();
                }

                // Initialize remaining quotes pool
                ResetQuotePool();
            }
            catch (Exception)
            {
                // If loading fails, use default quotes
                _quotes = GetDefaultQuotes();
                ResetQuotePool();
            }
        }

        private void ResetQuotePool()
        {
            _remainingQuotes = new List<string>(_quotes);
        }

        public string GetRandomQuote()
        {
            if (_quotes.Count == 0)
            {
                var culture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                return culture == "ru" ? "Сегодня — хороший день чтобы win!" : "Today is a good day to win!";
            }

            // If all quotes have been shown, reset the pool
            if (_remainingQuotes.Count == 0)
            {
                ResetQuotePool();
            }

            // Pick random quote from remaining pool
            var index = _random.Next(_remainingQuotes.Count);
            var quote = _remainingQuotes[index];
            
            // Remove from pool to avoid immediate repeats
            _remainingQuotes.RemoveAt(index);

            return quote;
        }

        private List<string> GetDefaultQuotes()
        {
            // Get current culture
            var culture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            
            if (culture == "ru")
            {
                return new List<string>
                {
                    "Сегодня я снова вынесу. Или хотя бы вынесут меня — тоже результат.",
                    "Удача в Дельте — как лут: слышал, что существует, но не видел.",
                    "Главное — не победа, а красиво умереть.",
                    "Мой баланс тает, но зато настроение стабильное: тоже тает.",
                    "Если рейд прошёл спокойно — это не рейд, это баг.",
                    "Я не проигрываю. Я просто даю другим шанс почувствовать себя победителями.",
                    "Победа — это когда ты хотя бы не последний.",
                    "Дельта учит смирению. Каждый день.",
                    "Сегодня я обязательно найду красную... В чужом рюкзаке.",
                    "Главное не тильтануть. Упс, поздно."
                };
            }
            else
            {
                // English quotes with British humor
                return new List<string>
                {
                    "Today I shall triumph. Or at least die with dignity. Probably just die.",
                    "Luck in Delta is like loot: theoretically exists, practically a myth.",
                    "It's not about winning, it's about making your death look tactical.",
                    "My balance is dropping faster than my K/D ratio.",
                    "If the raid went smoothly, it wasn't a raid — it was a bug.",
                    "I don't lose. I merely give others a fleeting moment of false hope.",
                    "Victory is when you're not the first one dead. That's growth.",
                    "Delta teaches humility. Repeatedly. Aggressively.",
                    "Today I'll definitely find a red... in someone else's backpack.",
                    "The key is not to tilt. Oh dear, too late."
                };
            }
        }

        private class QuoteData
        {
            [JsonProperty("quotes")]
            public List<string>? Quotes { get; set; }
        }

        public void Dispose()
        {
            // Clear managed resources
            _quotes?.Clear();
            _remainingQuotes?.Clear();
        }
    }
}
