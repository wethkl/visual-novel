using System;
using System.Collections.Generic;
using Verden.Story;

static class Program
{
    static int failures;

    static void Main()
    {
        List<Beat> beats = ChapterOne.Build();
        Expect(beats.Count > 40, "В главе слишком мало реплик: " + beats.Count);

        List<int> choiceCounts = new List<int>();
        WalkChoices(beats, delegate(Beat beat) { choiceCounts.Add(beat.Choices.Length); });
        Expect(choiceCounts.Count == 4, "Ожидались 4 выбора, получено " + choiceCounts.Count);
        if (choiceCounts.Count == 4)
        {
            Expect(choiceCounts[0] == 3 && choiceCounts[1] == 3 && choiceCounts[2] == 3 && choiceCounts[3] == 2,
                "Число веток не 3, 3, 3, 2.");
        }

        CheckPath(beats, new[] { 0, 0, 0, 0 }, 0, 1, 3, 4, true);
        CheckPath(beats, new[] { 2, 2, 2, 1 }, 0, 0, 1, 2, false);
        CheckPath(beats, new[] { 1, 1, 1, 1 }, 2, 1, 1, 1, false);

        for (int a = 0; a < 3; a++)
        for (int b = 0; b < 3; b++)
        for (int c = 0; c < 3; c++)
        for (int d = 0; d < 2; d++)
            Play(beats, new[] { a, b, c, d }, true);

        if (failures == 0)
        {
            Console.WriteLine("Глава I сходится: " + beats.Count + " реплик на основной линии, 54 пути без ошибок.");
            return;
        }

        Console.Error.WriteLine("Ошибок: " + failures);
        Environment.Exit(1);
    }

    static void CheckPath(List<Beat> beats, int[] plan, int attention, int agatha, int village, int suspicion, bool tail)
    {
        StorySession session = Play(beats, plan, true);
        Expect(session.Stats[Stat.Attention] == attention, "Внимательность " + session.Stats[Stat.Attention] + " вместо " + attention);
        Expect(session.Stats[Stat.AgathaTrust] == agatha, "Агата " + session.Stats[Stat.AgathaTrust] + " вместо " + agatha);
        Expect(session.Stats[Stat.VillageDebt] == village, "Деревня " + session.Stats[Stat.VillageDebt] + " вместо " + village);
        Expect(session.Stats[Stat.Suspicion] == suspicion, "Подозрение " + session.Stats[Stat.Suspicion] + " вместо " + suspicion);
        Expect(session.Flags.Contains(StoryFlags.AlienTail) == tail, "Флаг лоскута не совпал.");
    }

    static StorySession Play(List<Beat> beats, int[] plan, bool requireLine)
    {
        StorySession session = new StorySession();
        session.Begin(beats);
        int cursor = 0;
        bool sawLine = false;
        var faces = new HashSet<string> { Face.Mira, Face.Eleonora, Face.Sebastian, Face.Agatha, Face.Tomas, Face.None };
        var places = new HashSet<string> { Bg.Scriptorium, Bg.Office, Bg.Yard, Bg.Night, Bg.Title };

        while (!session.Finished)
        {
            Beat beat = session.Current;
            Expect(beat != null, "Пустая реплика.");
            Expect(!string.IsNullOrEmpty(beat.Text), "Пустой текст.");
            if (beat.Portrait != null) Expect(faces.Contains(beat.Portrait), "Неизвестный портрет " + beat.Portrait);
            if (!string.IsNullOrEmpty(beat.Background)) Expect(places.Contains(beat.Background), "Неизвестный фон " + beat.Background);
            if (beat.Text.IndexOf(ChapterOne.SebastianLine, StringComparison.Ordinal) >= 0) sawLine = true;

            if (beat.HasChoices)
            {
                Expect(cursor < plan.Length, "Выборов больше, чем в плане.");
                for (int i = 0; i < beat.Choices.Length; i++)
                    Expect(!string.IsNullOrEmpty(beat.Choices[i].Text), "Пустая кнопка выбора.");
                session.Choose(plan[cursor]);
                cursor++;
            }
            else session.Advance();
        }

        Expect(cursor == plan.Length, "Пройдено выборов: " + cursor);
        if (requireLine) Expect(sawLine, "Нет ключевой реплики Себастьяна.");
        foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            Expect(session.Stats[stat] >= 0, "Отрицательная шкала.");
        return session;
    }

    static void WalkChoices(List<Beat> beats, Action<Beat> onChoice)
    {
        void Visit(Beat beat)
        {
            if (!beat.HasChoices) return;
            onChoice(beat);
            for (int i = 0; i < beat.Choices.Length; i++)
            {
                Beat[] follow = beat.Choices[i].Follow;
                if (follow == null) continue;
                for (int j = 0; j < follow.Length; j++) Visit(follow[j]);
            }
        }

        for (int i = 0; i < beats.Count; i++) Visit(beats[i]);
    }

    static void Expect(bool condition, string message)
    {
        if (condition) return;
        Console.Error.WriteLine(message);
        failures++;
    }
}
