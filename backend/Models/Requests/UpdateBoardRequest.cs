using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Requests;

public class UpdateBoardRequest
{
    [Required]
    public string Title { get; init; } = null!;

    [Required]
    public HashSet<CreateRequestMember> Members { get; init; } = [];
}