﻿using D_Squared.Data.Context;
using D_Squared.Domain.Entities;
using D_Squared.Domain.TransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D_Squared.Data.Queries
{
    public class SalesDataQueries
    {
        private readonly D_SquaredDbContext db;
        public SalesDataQueries(D_SquaredDbContext db)
        {
            this.db = db;
        }
        public bool CheckForExistingSalesDataByDate(DateTime date, string storeNumber)
        {
            return db.LSSales.Any(sd => sd.BusinessDate == date && sd.Store == storeNumber);
        }

        public SalesDataDTO GetCurrentDaySales(string storeNumber)
        {
            DateTime currentDate = DateTime.Now.Date;
            LSSales sData = db.LSSales.Where(sd => sd.BusinessDate == currentDate && sd.Store.StartsWith(storeNumber)).FirstOrDefault();
            SalesDataDTO sdDTO = new SalesDataDTO(sData);
            return sdDTO;
        }

        public SalesDataDTO GetSelectedBusinessDaySales(DateTime businessDate, string storeNumber)
        {
            LSSales sData = db.LSSales.Where(sd => sd.BusinessDate == businessDate.Date && sd.Store.StartsWith(storeNumber)).FirstOrDefault();
            SalesDataDTO sdDTO = new SalesDataDTO(sData);
            return sdDTO;
        }

        public List<WeeklyTotalDurationDTO> GetWeeklyTotalDurationDTOs(string storeNumber, DateTime weekEnding)
        {
            List<WeeklyTotalDurationDTO> weeklyTotalDurationDTOs = db.WeeklyTotalDurations.Where(w => w.StoreNumber == storeNumber && w.WeekEnding == weekEnding && w.TotalDuration > 35)
                                                                    .Select(w => new WeeklyTotalDurationDTO
                                                                    {
                                                                        WeekEnding = w.WeekEnding,
                                                                        StoreNumber = w.StoreNumber,
                                                                        StaffName = w.StaffName,
                                                                        TotalDuration = w.TotalDuration
                                                                    }).ToList();

            return weeklyTotalDurationDTOs;
        }

        public List<WeeklyTotalDurationDTO>  GetWeeklyTotalDurationDTOsByJob(string job, string storeNumber, DateTime weekEnding)
        {
            List<WeeklyTotalDurationDTO> weeklyTotalDurationDTOs = db.WeeklyTotalDurations.Where(w => w.StoreNumber == storeNumber && w.WeekEnding == weekEnding /*&& w.Job == job*/)
                                                                    .Select(w => new WeeklyTotalDurationDTO
                                                                    {
                                                                        WeekEnding = w.WeekEnding,
                                                                        StoreNumber = w.StoreNumber,
                                                                        StaffName = w.StaffName,
                                                                        TotalDuration = w.TotalDuration
                                                                    }).ToList();

            return weeklyTotalDurationDTOs;
        }

        public List<EmployeeJobDTO> GetDistinctJobNames(string storeNumber)
        {
            List<EmployeeJobDTO> jobs = db.EmployeeJobs.Where(j => j.StoreNumber == storeNumber).Distinct()
                                        .Select(j => new EmployeeJobDTO
                                        {
                                            Job = j.Job
                                        }).ToList();
            return jobs;
        }
    }
}
