﻿using D_Squared.Domain.TransferObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace D_Squared.Web.Models
{
    public class LaborSummaryViewModel
    {
        public EmployeeDTO EmployeeInfo { get; set; }
    }

    public class LaborSummarySearchViewModel
    {
        public LaborDataSearchDTO SearchDTO { get; set; }

        public List<LaborDataDTO> SearchResults { get; set; }

        public EmployeeDTO EmployeeInfo { get; set; }

        [Display(Name = "Location")]
        public List<SelectListItem> LocationSelectList { get; set; }

        public DateTime BusinessWeekStartDate { get; set; }

        public DateTime BusinessWeekEndDate { get; set; }
    }

}