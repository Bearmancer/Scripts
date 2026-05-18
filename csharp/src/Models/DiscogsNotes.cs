namespace CSharpScripts.Models;

internal record ParsedNotes(
	IReadOnlyList<string> Composers,
	IReadOnlyList<string> Conductors,
	IReadOnlyList<string> Orchestras,
	IReadOnlyList<string> Venues,
	IReadOnlyList<RecordingDate> RecordingDates,
	IReadOnlyList<TrackAnnotation> TrackAnnotations,
	string RawNotes
);

internal record RecordingDate(string Description, DateOnly? Date);

internal record TrackAnnotation(string TrackReference, string Annotation);


