using System;
using System.Collections.Generic;
using System.Text;

namespace MauiEmployeeApp.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string FullName { get; set; }

        public string position { get; set; }

        public decimal Salary { get; set; }
    }
}
