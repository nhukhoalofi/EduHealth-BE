namespace EduHealth.DTOs.Messaging
{
    public class CreateConversationRequestDto
    {
        public int ParticipantUserId { get; set; }
        public int? StudentId { get; set; }
    }
}
