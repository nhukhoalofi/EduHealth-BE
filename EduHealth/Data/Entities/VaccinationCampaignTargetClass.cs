namespace EduHealth.Data.Entities
{
    public class VaccinationCampaignTargetClass
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public int ClassId { get; set; }

        public VaccinationCampaign Campaign { get; set; } = null!;
        public SchoolClass Class { get; set; } = null!;
    }
}
