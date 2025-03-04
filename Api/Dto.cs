namespace Api;

public class Dto
{
    public class GameDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<PlayerDto> Players { get; set; } = new();
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class PlayerDto
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
    }

    public class QuestionDto
    {
        public string Id { get; set; }
        public string QuestionText { get; set; }
        public bool Answered { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    public class QuestionOptionDto
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}