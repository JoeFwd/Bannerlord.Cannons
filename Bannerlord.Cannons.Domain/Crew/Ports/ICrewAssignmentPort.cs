namespace Bannerlord.Cannons.Domain.Crew
{
    public interface ICrewAssignmentPort
    {
        CrewAssignmentRequest CreateRequest();

        void ApplyAssignments(CrewAssignmentResult result);
    }
}
