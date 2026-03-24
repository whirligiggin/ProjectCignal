using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectCignal.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProjectCignal.Pages;

public class IndexModel : PageModel
{
    private readonly TripEstimatorService _tripEstimatorService;

    public IndexModel(TripEstimatorService tripEstimatorService)
    {
        _tripEstimatorService = tripEstimatorService;
    }

    [BindProperty]
    public TripInput Input { get; set; } = new();

    public TripResult? Result { get; set; }

    public void OnGet()
    {
        PresetSegments = _tripEstimatorService.GetPresetSegments();
    }

    public void OnPost()
    {
        PresetSegments = _tripEstimatorService.GetPresetSegments();
        
        if (!string.IsNullOrEmpty(Input.SelectedSegment))
        {
            var segment = PresetSegments.FirstOrDefault(s => s.Name == Input.SelectedSegment);
            if (segment != null)
            {
                Input.SegmentName = segment.Name;
                Input.DistanceMiles = segment.DistanceMiles;
                Input.RiverCurrentMph = segment.DefaultRiverCurrentMph;

                ModelState.Remove("Input.SegmentName");
                ModelState.Remove("Input.DistanceMiles");
                ModelState.Remove("Input.RiverCurrentMph");
            }
        }
        if (!ModelState.IsValid)
        {
            return;
        }
        
        try
        {
            var estimate = _tripEstimatorService.Estimate(
                Input.SegmentName,
                Input.DistanceMiles,
                Input.PaddlingSpeedMph,
                Input.RiverCurrentMph,
                Input.LaunchTime);

            Result = new TripResult
            {
                SegmentName = estimate.SegmentName,
                EstimatedDurationText = $"{(int)estimate.EstimatedDuration.TotalHours} hr {estimate.EstimatedDuration.Minutes} min",
                EstimatedFinishTimeText = estimate.EstimatedFinishTime.HasValue
                    ? estimate.EstimatedFinishTime.Value.ToString("f")
                    : "No launch time provided",
                Assumptions = estimate.Assumptions
            };
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
    }

    public class TripInput
    {
        [Display(Name = "Preset Segment")]
        public string? SelectedSegment { get; set; }

        [Display(Name = "River Segment")]
        [Required]
        public string SegmentName { get; set; } = string.Empty;

        [Display(Name = "Distance (miles)")]
        [Range(0.1, 500)]
        public double DistanceMiles { get; set; }

        [Display(Name = "Paddling Speed (mph)")]
        [Range(0.1, 20)]
        public double PaddlingSpeedMph { get; set; }

        [Display(Name = "River Current (mph)")]
        [Range(-10, 20)]
        public double RiverCurrentMph { get; set; }

        [Display(Name = "Launch Time")]
        [DataType(DataType.DateTime)]
        public DateTime? LaunchTime { get; set; }
    }

    public class TripResult
    {
        public string SegmentName { get; set; } = string.Empty;
        public string EstimatedDurationText { get; set; } = string.Empty;
        public string EstimatedFinishTimeText { get; set; } = string.Empty;
        public string Assumptions { get; set; } = string.Empty;
    }

    public List<ProjectCignal.Services.RiverSegment> PresetSegments { get; set; } = new();
}