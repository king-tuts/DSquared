﻿using D_Squared.Data.Millers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D_Squared.Data.Millers.Queries
{
    public class ForecastDataQueries
    {
        private readonly ForecastDataDbContext db;

        public ForecastDataQueries(ForecastDataDbContext db)
        {
            this.db = db;
        }

        //dummy queries
        public decimal GetSalesPriorYear(string storeNumber, DateTime day)
        {
            if (db.ForecastData.Any(fd => fd.StoreNumber == storeNumber && fd.BusinessDate == day))
                return db.ForecastData.Where(fd => fd.StoreNumber == storeNumber && fd.BusinessDate == day).FirstOrDefault().ActualPriorYear;
            else
                return new decimal(0);
        }

        public decimal GetAverageSalesPerMonth(string storeNumber, DateTime day)
        {
            if (db.ForecastData.Any(fd => fd.StoreNumber == storeNumber && fd.BusinessDate == day))
                return db.ForecastData.Where(fd => fd.StoreNumber == storeNumber && fd.BusinessDate == day).FirstOrDefault().AvgPrior4Weeks;
            else
                return new decimal(0);  
        }

        public decimal GetLaborForecast(string storeNumber, DateTime day)
        {
            if (db.ForecastData.Any(fd => fd.StoreNumber == storeNumber && fd.BusinessDate == day))
                return db.ForecastData.Where(fd => fd.StoreNumber == storeNumber && fd.BusinessDate == day).FirstOrDefault().LaborForecast;
            else
                return new decimal(0);       
        }
    }
}
