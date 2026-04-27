using System;
using System.Collections.Generic;
using System.Text;

using SQLite;

namespace SparkWork2.Models;

public class CandidateProfile
{
    [PrimaryKey]
    public int CandidateId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
}