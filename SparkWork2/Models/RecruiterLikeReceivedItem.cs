using System;
using System.Collections.Generic;
using System.Text;

namespace SparkWork2.Models;

public class RecruiterLikeReceivedItem
{
    public int CandidateUserId { get; set; }
    public string CandidateName { get; set; }

    public int JobOfferId { get; set; }
    public string JobTitle { get; set; }
}