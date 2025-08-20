public record Superhero(string Name, string? RealName, Look Look)
{
    public static Superhero[] Superheroes =
    [
        new("Superman", "Clark Kent", new([Color.Blue, Color.Red])),
        new("Batman", "Bruce Wayne", new Look([Color.Black, Color.DarkGray])),
        new("Aquaman", "Arthur Curry", new Look([Color.Yellow, Color.Green])),
        new("Wonder Woman", "Diana Prince", new Look([Color.Gold, Color.Black])),
        new("Shazam", "Billy Batson", new Look([Color.Red, Color.Green])),
        new("Vigilante", "Adrian Chase", new Look([Color.Black])),
        new("Supergirl", "Kara Zor-El", new([Color.Blue, Color.Red])),
        new("Atom Smasher", "Dinah Lance", new Look([])),
        new("Martian Manhunter", "Calvin Swanwick", new Look([Color.Green, Color.Black])),
        new("Captain America", "Steve Rogers", new Look([Color.White, Color.Blue])),
        new("Iron Man", "Anthony Edward Tony Stark", new([Color.Blue, Color.Red])),
        new("Thor", "Thor", new Look([Color.Gold, Color.Blue])),
        new("Hulk", "Robert Bruce Banner", new Look([Color.Green])),
        new("Black Widow", "Natasha Romanoff", new Look([Color.Black])),
        new("Hawkeye", "Clinton Francis \"Clint\" Barton", new Look([Color.Black, Color.Green])),
        new("Scarlet Witch", "Wanda Maximoff", new Look([Color.Purple])),
        new("Vision", "Vision", new Look([Color.Gold, Color.Purple])),
        new("Falcon", "Sam Wilson", new Look([Color.Blue, Color.White])),
        new("Winter Soldier", "Bucky Barnes", new Look([Color.Black])),
        new("War Machine", "James Rupert \"Rhodey\" Rhodes", new Look([Color.White, Color.Black])),
        new("Sif", "Sif", new Look([Color.Gold])),
        new("Valkyrie", "Brunnhilde", new Look([Color.Gold])),
        new("Ant-Man", "Scott Edward Harris Lang", new Look([Color.Red, Color.Black])),
        new("Wasp", "Janet Van Dyne", new Look([Color.Gold, Color.Black])),
        new("Doctor Strange", "Dr. Stephen Vincent Strange", new Look([Color.Red, Color.DarkRed])),
        new("Spider-Man", "Peter Parker", new Look([Color.Blue, Color.Red])),
        new("Blade", "Eric Brooks", new Look([Color.Black])),
        new("Daredevil", "Matt Murdock", new Look([Color.Red, Color.Black])),
        new("Nick Fury", "Nick Fury", new Look([Color.Black])),
        new("Deadpool", "Wade Wilson", new Look([Color.Red, Color.Black])),
        new("Wolverine", "James Howlett", new Look([Color.Yellow, Color.Black])),
        new("Venom", "Edward Charles Allan", new Look([Color.Black])),
        new("Silver Surfer", "Norrin Radd", new Look([Color.Silver])),
        new("Green Lantern", "Alan Scott", new Look([Color.Green])),
        new("Hawkman", "Carter Hall", new Look([Color.Gold, Color.Black])),
        new("Hawkgirl", "Kendra Saunders", new Look([Color.Gold, Color.Black])),
        new("Nightwing", "Richard John \"Dick\" Grayson", new Look([Color.Black])),
        new("Robin", "Richard John \"Dick\" Grayson", new Look([Color.Red, Color.Yellow])),
        new("Batgirl", "Barbara Gordon", new Look([Color.Black])),
        new("X", null, new([Color.Blue, Color.Red])),
    ];
}

public record Look(Color[] Colors);

public enum Color
{
    Black,
    Red,
    Green,
    Blue,
    Purple,
    White,
    Yellow,
    Gold,
    Silver,
    DarkRed,
    DarkGreen,
    DarkBlue,
    DarkPurple,
    DarkGray
}