using System;
using System.Collections.Generic;
using System.Text;

using SQLite;

namespace SparkWork2.Models;

public class CandidateProfile
{
    [PrimaryKey]
    public int CandidateId { get; set; }

    public string FullName { get; set; }
    public string Title { get; set; }
    public string Location { get; set; }
    public string About { get; set; }
    public string Email { get; set; }
    public string? PhotoPath { get; set; }
}