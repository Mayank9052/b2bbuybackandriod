using System;

namespace B2BBuyback.Api.Models;

public partial class ExchangeCaseDocument
{
    public int    Id             { get; set; }
    public int    CaseId         { get; set; }

    // RC | IDProof | PaymentProof | Insurance |
    // Hypothecation | LoanNOC | ServiceHistory
    public string DocumentType   { get; set; } = null!;

    // Relative URL served by static files: /CaseDocuments/{caseId}/RC.pdf
    public string FilePath       { get; set; } = null!;

    // Original filename the dealer uploaded
    public string FileName       { get; set; } = null!;

    // MIME type stored for download Content-Type header
    public string? ContentType   { get; set; }

    public long    FileSizeBytes  { get; set; }
    public DateTime UploadedAt   { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ExchangeCase Case { get; set; } = null!;
}