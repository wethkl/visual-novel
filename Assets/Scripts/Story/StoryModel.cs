using System;
using System.Collections.Generic;

namespace Verden.Story
{
    public enum Stat
    {
        Attention,
        AgathaTrust,
        VillageDebt,
        Suspicion
    }

    public struct StatDelta
    {
        public Stat Stat;
        public int Delta;

        public StatDelta(Stat stat, int delta)
        {
            Stat = stat;
            Delta = delta;
        }
    }

    public static class StatNames
    {
        public static string Of(Stat stat)
        {
            switch (stat)
            {
                case Stat.Attention: return "Внимательность";
                case Stat.AgathaTrust: return "Доверие Агаты";
                case Stat.VillageDebt: return "Долг деревни";
                case Stat.Suspicion: return "Подозрение Себастьяна";
                default: return stat.ToString();
            }
        }
    }

    public static class StoryFlags
    {
        public const string AlienTail = "alien_tail";
    }

    public static class Bg
    {
        public const string Title = "title";
        public const string Scriptorium = "scriptorium";
        public const string Office = "office";
        public const string Yard = "yard";
        public const string Night = "night";
    }

    public static class Face
    {
        public const string Mira = "mira";
        public const string Eleonora = "eleonora";
        public const string Sebastian = "sebastian";
        public const string Agatha = "agata";
        public const string Tomas = "tomas";
        public const string None = "";
    }

    public sealed class Choice
    {
        public string Text;
        public StatDelta[] Effects;
        public string Flag;
        public Beat[] Follow;

        public Choice(string text, Beat[] follow, params StatDelta[] effects)
        {
            Text = text;
            Follow = follow;
            Effects = effects;
            Flag = null;
        }

        public Choice(string text, string flag, Beat[] follow, params StatDelta[] effects)
        {
            Text = text;
            Flag = flag;
            Follow = follow;
            Effects = effects;
        }
    }

    public sealed class Beat
    {
        public string Speaker;
        public string Text;
        public string Portrait;
        public string Background;
        public Choice[] Choices;

        public bool HasChoices
        {
            get { return Choices != null && Choices.Length > 0; }
        }

        public static Beat Narration(string text, string background, string portrait)
        {
            return new Beat
            {
                Speaker = "",
                Text = text,
                Background = background,
                Portrait = Face.None,
                Choices = null
            };
        }

        public static Beat Say(string speaker, string text, string portrait, string background, Choice[] choices)
        {
            return new Beat
            {
                Speaker = speaker,
                Text = text,
                Portrait = portrait,
                Background = background,
                Choices = choices
            };
        }
    }

    public sealed class StorySession
    {
        readonly Queue<Beat> upcoming = new Queue<Beat>();

        public Beat Current { get; private set; }
        public bool Finished { get; private set; }
        public Dictionary<Stat, int> Stats { get; private set; }
        public HashSet<string> Flags { get; private set; }
        public IList<StatDelta> LastEffects { get; private set; }

        public bool HasChoices
        {
            get { return Current != null && Current.HasChoices; }
        }

        public void Begin(IList<Beat> beats)
        {
            upcoming.Clear();
            for (int i = 0; i < beats.Count; i++) upcoming.Enqueue(beats[i]);
            Stats = new Dictionary<Stat, int>();
            foreach (Stat stat in Enum.GetValues(typeof(Stat))) Stats[stat] = 0;
            Flags = new HashSet<string>();
            LastEffects = new List<StatDelta>();
            Finished = false;
            Current = null;
            Pull();
        }

        public void Advance()
        {
            if (Finished || HasChoices) return;
            Pull();
        }

        public void Choose(int index)
        {
            if (!HasChoices) throw new InvalidOperationException("Сейчас нет выбора.");
            Choice[] options = Current.Choices;
            if (index < 0 || index >= options.Length) throw new ArgumentOutOfRangeException("index");

            Choice choice = options[index];
            List<StatDelta> applied = new List<StatDelta>();
            if (choice.Effects != null)
            {
                for (int i = 0; i < choice.Effects.Length; i++)
                {
                    StatDelta effect = choice.Effects[i];
                    Stats[effect.Stat] = Stats[effect.Stat] + effect.Delta;
                    applied.Add(effect);
                }
            }

            if (!string.IsNullOrEmpty(choice.Flag)) Flags.Add(choice.Flag);
            LastEffects = applied;

            if (choice.Follow != null && choice.Follow.Length > 0)
            {
                Beat[] rest = upcoming.ToArray();
                upcoming.Clear();
                for (int i = 0; i < choice.Follow.Length; i++) upcoming.Enqueue(choice.Follow[i]);
                for (int i = 0; i < rest.Length; i++) upcoming.Enqueue(rest[i]);
            }

            Pull();
        }

        void Pull()
        {
            if (upcoming.Count == 0)
            {
                Current = null;
                Finished = true;
                return;
            }

            Current = upcoming.Dequeue();
            Finished = false;
        }
    }
}
