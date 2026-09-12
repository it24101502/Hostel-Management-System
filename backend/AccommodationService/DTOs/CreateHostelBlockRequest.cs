using System.ComponentModel.DataAnnotations;

namespace AccommodationService.DTOs;

public class CreateHostelBlockRequest
{
    [Required(ErrorMessage = "Block code is required.")]
    [StringLength(
        20,
        ErrorMessage = "Block code cannot exceed 20 characters.")]
    public string BlockCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Block name is required.")]
    [StringLength(
        100,
        ErrorMessage = "Block name cannot exceed 100 characters.")]
    public string BlockName { get; set; } = string.Empty;
}