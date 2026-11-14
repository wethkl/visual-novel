using System.Collections.Generic;

namespace Verden.Story
{
    public sealed class ChapterInfo
    {
        public int Number;
        public string Title;
        public string Teaser;
        public bool Playable;
        public List<Beat> Beats;
    }

    public static class StoryCatalog
    {
        public static ChapterInfo[] Chapters()
        {
            return new[]
            {
                new ChapterInfo
                {
                    Number = 1,
                    Title = "Чужая подпись",
                    Teaser = "Две книги, чужая буква и арест старосты Липовки.",
                    Playable = true,
                    Beats = ChapterOne.Build()
                },
                new ChapterInfo
                {
                    Number = 2,
                    Title = "Те, кого не замечают",
                    Teaser = "Кухня, Лина и дорога к Нюте.",
                    Playable = false,
                    Beats = null
                },
                new ChapterInfo
                {
                    Number = 3,
                    Title = "Две бумаги",
                    Teaser = "Настоящий указ, имя отца и больница.",
                    Playable = false,
                    Beats = null
                },
                new ChapterInfo
                {
                    Number = 4,
                    Title = "Право говорить",
                    Teaser = "Мешок зерна, донос и вызов к герцогине.",
                    Playable = false,
                    Beats = null
                },
                new ChapterInfo
                {
                    Number = 5,
                    Title = "Суд герба",
                    Teaser = "Одна сцена суда и три концовки.",
                    Playable = false,
                    Beats = null
                }
            };
        }
    }
}
