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
    public class LaborDataQueries
    {
        private readonly D_SquaredDbContext db;
        public LaborDataQueries(D_SquaredDbContext db)
        {
            this.db = db;
        }

        public List<WeeklyTotalDurationDTO> GetWeeklyTotalDurationDTOs(string storeNumber, DateTime weekEnding, int hours)
        {
            List<WeeklyTotalDurationDTO> weeklyTotalDurationDTOs = db.WeeklyTotalDurations.Where(w => w.StoreNumber == storeNumber && w.WeekEnding == weekEnding && w.TotalDuration > hours)
                                                                    .Select(w => new WeeklyTotalDurationDTO
                                                                    {
                                                                        WeekEnding = w.WeekEnding,
                                                                        StoreNumber = w.StoreNumber,
                                                                        StaffName = w.StaffName,
                                                                        TotalDuration = w.TotalDuration,
                                                                        Overtime = w.TotalDuration - hours
                                                                    }).ToList();

            return weeklyTotalDurationDTOs;
        }

        public List<WeeklyTotalDurationDTO> GetWeeklyTotalDurationDTOsByJob(string job, string storeNumber, DateTime weekEnding, int hours)
        {
            List<WeeklyTotalDurationDTO> weeklyTotalDurationDTOs = db.WeeklyTotalDurations.Where(w => w.StoreNumber == storeNumber && w.WeekEnding == weekEnding && w.TotalDuration > hours /*&& w.Job == job*/)
                                                                    .Select(w => new WeeklyTotalDurationDTO
                                                                    {
                                                                        WeekEnding = w.WeekEnding,
                                                                        StoreNumber = w.StoreNumber,
                                                                        StaffName = w.StaffName,
                                                                        TotalDuration = w.TotalDuration,
                                                                        Overtime = w.TotalDuration - hours
                                                                    }).ToList();

            return weeklyTotalDurationDTOs;
        }

        public List<LaborDataDTO> GetLaborDataByDayAndJob(string storeNumber, DateTime businessDate)
        {
            var lsLaborGroup = db.LSLabors.Where(ld => ld.BusinessDate == businessDate.Date && ld.Store == storeNumber)
                                                .GroupBy(ld => ld.JobName);


            return BuildLaborDataDTOs(lsLaborGroup, "Job");
        }

        public List<LaborDataDTO> GetLaborDataByDayAndCenter(string storeNumber, DateTime businessDate)
        {
            var lsLaborGroup = db.LSLabors.Where(ld => ld.BusinessDate == businessDate.Date && ld.Store == storeNumber)
                                        .GroupBy(ld => ld.Center);

            return BuildLaborDataDTOs(lsLaborGroup, "Center");
        }

        public List<LaborDataDTO> GetLaborDataByWeekAndJob(string storeNumber, DateTime startDate, DateTime endDate)
        {
            DateTime realEndDate = endDate.AddDays(1);
            var lsLaborGroup = db.LSLabors.Where(ld => ld.BusinessDate >= startDate && ld.BusinessDate < realEndDate && ld.Store == storeNumber)
                                        .GroupBy(ld => ld.JobName);

            return BuildLaborDataDTOs(lsLaborGroup, "Job");
        }

        public List<LaborDataDTO> GetLaborDataByWeekAndCenter(string storeNumber, DateTime startDate, DateTime endDate)
        {
            DateTime realEndDate = endDate.AddDays(1);
            var lsLaborGroup = db.LSLabors.Where(ld => ld.BusinessDate >= startDate && ld.BusinessDate < realEndDate && ld.Store == storeNumber)
                                        .GroupBy(ld => ld.Center);

            return BuildLaborDataDTOs(lsLaborGroup, "Center");
        }

        private List<LaborDataDTO> BuildLaborDataDTOs(IQueryable<IGrouping<string, LSLabor>> lsLaborGroup, string reportType)
        {
            List<LaborDataDTO> laborDataDTOs = new List<LaborDataDTO>();
            foreach (var ldGroup in lsLaborGroup)
            {
                LaborDataDTO lDataDTO = new LaborDataDTO
                {
                    RegularHours = ldGroup.Sum(ld => ld.RegularHours),
                    OTHours = ldGroup.Sum(ld => ld.OTHours),
                    RegularPayAmount = ldGroup.Sum(ld => ld.RegularPayAmount),
                    OTPayAmount = ldGroup.Sum(ld => ld.OTPayAmount),
                    TotalHours = ldGroup.Sum(ld => ld.TotalHours),
                    TotalPayAmount = ldGroup.Sum(ld => ld.TotalPayAmount)
                };
                if (reportType == "Job")
                {
                    lDataDTO.JobName = ldGroup.Key;
                }
                else
                {
                    lDataDTO.Center = ldGroup.Key;
                }

                laborDataDTOs.Add(lDataDTO);
            }

            return laborDataDTOs;
        }

    }
}
