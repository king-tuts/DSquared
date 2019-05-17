﻿using D_Squared.Data.Millers.Queries;
using D_Squared.Data.Queries;
using D_Squared.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using D_Squared.Domain.Entities;
using D_Squared.Domain.TransferObjects;
using Newtonsoft.Json;
using System.Configuration;
using D_Squared.Domain;
using System.Globalization;

namespace D_Squared.Web.Helpers
{
    public class RedbookEntryInitializer
    {
        private readonly RedbookEntryQueries rbeq;
        private readonly CodeQueries cq;
        private readonly SalesForecastQueries sfq;
        private readonly SalesDataQueries sd;
        private readonly EmployeeQueries eq;

        public RedbookEntryInitializer(RedbookEntryQueries rbeq, CodeQueries cq, SalesForecastQueries sfq, SalesDataQueries sd, EmployeeQueries eq)
        {
            this.rbeq = rbeq;
            this.cq = cq;
            this.sfq = sfq;
            this.sd = sd;
            this.eq = eq;
        }

        #region Helpers
        protected List<string> GetPastWeek()
        {
            return Enumerable.Range(0, 7).Select(i => DateTime.Now.ToLocalTime().Date.AddDays(-i).ToShortDateString()).ToList();
        }

        protected List<string> GetCurrentWeek(DateTime selectedDay)
        {
            int currentDayOfWeek = (int)selectedDay.DayOfWeek;
            DateTime sunday = selectedDay.AddDays(-currentDayOfWeek);
            DateTime monday = sunday.AddDays(1);

            if (currentDayOfWeek == 0)
            {
                monday = monday.AddDays(-7);
            }
            var dates = Enumerable.Range(0, 7).Select(days => monday.AddDays(days).ToShortDateString()).ToList();

            return dates;
        }

        protected List<EventDTO> CreateEventDtos(List<string> eventCodeList, List<string> selectedEvents)
        {
            List<EventDTO> eventPairs = new List<EventDTO>();

            foreach (var eventCode in eventCodeList)
            {
                if (selectedEvents.Contains(eventCode))
                    eventPairs.Add(new EventDTO() { Event = eventCode, IsChecked = true });
                else
                    eventPairs.Add(new EventDTO() { Event = eventCode, IsChecked = false });
            }

            return eventPairs;
        }

        protected List<EventDTO> CreateEventDtos(List<string> eventCodeList, List<RedbookSalesEvent> selectedEvents)
        {
            List<EventDTO> eventPairs = new List<EventDTO>();

            foreach (var eventCode in eventCodeList)
            {
                if (selectedEvents.Select(se => se.Event).Contains(eventCode))
                    eventPairs.Add(new EventDTO() { Event = eventCode, IsChecked = true });
                else
                    eventPairs.Add(new EventDTO() { Event = eventCode, IsChecked = false });
            }

            return eventPairs;
        }

        protected DateTime TryParseDateTimeString(string date)
        {
            DateTime.TryParse(date, out DateTime convertedDate);

            return convertedDate;
        }

        protected string SerializeSelectedEventDTOs(List<EventDTO> list)
        {
            return JsonConvert.SerializeObject(list);
        }

        protected List<EventDTO> DeserializeSelectedEvents(string selectedEvents)
        {
            return JsonConvert.DeserializeObject<List<EventDTO>>(selectedEvents);
        }

        protected List<string> GetLocationList(EmployeeDTO employee, bool isRegional, bool isDivisional, bool isAdmin)
        {
            return isRegional ? eq.GetStoreLocationListByRegion(employee)
                                        : isDivisional ? eq.GetStoreLocationListByDivision(employee)
                                        : isAdmin ? eq.GetStoreLocationListForAdmin()
                                        : new List<string>();
        }
        #endregion

        public RedbookEntry BindPostValuesToEntity(RedbookEntry redbookEntry, string selectedDateString, string selectedStoreNumber)
        {
            redbookEntry.BusinessDate = TryParseDateTimeString(selectedDateString);
            redbookEntry.LocationId = selectedStoreNumber;

            return redbookEntry;
        }

        public RedbookEntryBaseViewModel InitializeBaseViewModel(string selectedDate, string storeNumber, string userName)
        {
            DateTime currentDate = DateTime.Today.ToLocalTime();
            DateTime convertedSelectedDate = TryParseDateTimeString(selectedDate);

            RedbookEntry redbookEntry = rbeq.GetExistingOrSeedEmpty(selectedDate, storeNumber, userName);

            RedbookEntryBaseViewModel model = new RedbookEntryBaseViewModel()
            {
                SelectedDateString = selectedDate,
                DateSelectList = GetCurrentWeek(currentDate).ToSelectList(selectedDate),
                EndingPeriod = GetCurrentWeek(currentDate).Last(),
                SelectedLocation = storeNumber,
                LocationSelectList = eq.GetLocationList().ToSelectList(storeNumber),
                EmployeeInfo = eq.GetEmployeeInfo(userName),
                SalesForecastDTO = sfq.GetLiveSalesForecastDTO(convertedSelectedDate, storeNumber),
                SalesDataDTO = sd.GetCurrentDaySales(storeNumber),
                EventDTOs = CreateEventDtos(cq.GetDistinctListByCodeCategory("Event"), redbookEntry.SalesEvents == null ? new List<RedbookSalesEvent>() : redbookEntry.SalesEvents.ToList()),
                WeatherSelectListAM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(null, true, "N/A"),
                WeatherSelectListPM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(null, true, "N/A"),
                ManagerSelectListAM = eq.GetManagersForLocation(storeNumber).ToSelectList("sAMAccountName", "FullName", null, true, "--Select--", string.Empty),
                ManagerSelectListPM = eq.GetManagersForLocation(storeNumber).ToSelectList("sAMAccountName", "FullName", null, true, "--Select--", string.Empty),
                RedbookEntry = redbookEntry,
                TicketURL = ConfigurationManager.AppSettings["RedbookTicketURL"],
                CompetitiveEventListViewModel = new CompetitiveEventListViewModel(rbeq.GetCompetitiveEvents(redbookEntry.Id, redbookEntry.LocationId))
            };

            return model;
        }

        public SalesDataDTO InitializeSalesDataDTO(string storeNumber)
        {
            return sd.GetCurrentDaySales(storeNumber);
        }

        public RedbookEntryBaseViewModel InitializeBaseViewModel(RedbookEntryBaseViewModel model, string userName)
        {
            DateTime currentDate = DateTime.Today.ToLocalTime();

            model.DateSelectList = GetPastWeek().ToSelectList(currentDate.ToShortDateString());
            model.LocationSelectList = eq.GetLocationList().ToSelectList(model.RedbookEntry.LocationId);
            model.WeatherSelectListAM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(model.RedbookEntry.SelectedWeatherAM, true, "N/A");
            model.WeatherSelectListPM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(model.RedbookEntry.SelectedWeatherPM, true, "N/A");
            model.ManagerSelectListAM = eq.GetManagersForLocation(model.RedbookEntry.LocationId).ToSelectList("sAMAccountName", "FullName", model.RedbookEntry.ManagerOnDutyAM, true, "--Select--", string.Empty);
            model.ManagerSelectListPM = eq.GetManagersForLocation(model.RedbookEntry.LocationId).ToSelectList("sAMAccountName", "FullName", model.RedbookEntry.ManagerOnDutyPM, true, "--Select--", string.Empty);

            model.EmployeeInfo = eq.GetEmployeeInfo(userName);
            model.SalesForecastDTO = sfq.GetLiveSalesForecastDTO(model.RedbookEntry.BusinessDate, model.RedbookEntry.LocationId);

            model.TicketURL = ConfigurationManager.AppSettings["RedbookTicketURL"];
            model.EndingPeriod = GetCurrentWeek(currentDate).Last();

            model.CompetitiveEventListViewModel = new CompetitiveEventListViewModel(rbeq.GetCompetitiveEvents(model.RedbookEntry.Id, model.RedbookEntry.LocationId));

            return model;
        }

        public CompetitiveEventCreateEditViewModel InitializeCompetitiveEventCreateEditViewModel(int redbookId)
        {
            RedbookEntry parentEntry = rbeq.FindById(redbookId);

            return new CompetitiveEventCreateEditViewModel(parentEntry.BusinessDate, parentEntry.LocationId, redbookId)
            {
                DistanceRanges = DomainConstants.CompetitiveEventConstants.DistanceRanges().ToSelectList(null)
            };
        }

        public CompetitiveEventListViewModel InitializeCompetitiveEventListViewModel(int redbookId, string storeNumber)
        {
            return new CompetitiveEventListViewModel(rbeq.GetCompetitiveEvents(redbookId, storeNumber));
        }

        public CompetitiveEventCreateEditViewModel InitializeCompetitiveEventCreateEditSelectLists(CompetitiveEventCreateEditViewModel model)
        {
            model.DistanceRanges = DomainConstants.CompetitiveEventConstants.DistanceRanges().ToSelectList(model.CompetitiveEvent.Distance);

            return model;
        }

        public RedbookEntryDetailPartialViewModel InitializeRedbookEntryDetailPartialViewModel(int redbookId, string userName, bool isLastYear, string date = "")
        {
            RedbookEntry redbookEntry = redbookId > 0 ? rbeq.FindById(redbookId) : rbeq.GetExistingOrSeedEmpty(date, eq.GetEmployeeInfo(userName).StoreNumber, userName);
            RedbookEntry lastYearRedbook = rbeq.GetExistingOrSeedEmpty(isLastYear ? redbookEntry.BusinessDate.AddDays(-364).ToShortDateString() : redbookEntry.BusinessDate.ToShortDateString(), redbookEntry.LocationId, userName);

            RedbookEntryDetailPartialViewModel model = new RedbookEntryDetailPartialViewModel()
            {
                RedbookEntry = lastYearRedbook,
                SalesForecastDTO = sfq.GetLiveSalesForecastDTO(lastYearRedbook.BusinessDate, lastYearRedbook.LocationId),
                EventDTOs = CreateEventDtos(cq.GetDistinctListByCodeCategory("Event"), lastYearRedbook.SalesEvents == null ? new List<RedbookSalesEvent>() : lastYearRedbook.SalesEvents.ToList()).Where(e => e.IsChecked).ToList(),
                CompetitiveEventListViewModel = new CompetitiveEventListViewModel(rbeq.GetCompetitiveEvents(lastYearRedbook.Id, lastYearRedbook.LocationId))
            };

            return model;
        }

        public RedbookEntrySearchViewModel InitializeRedbookEntrySearchViewModel(EmployeeDTO employee, bool isRegional, bool isDivisional, bool isAdmin)
        {
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin);

            RedbookEntrySearchViewModel model = new RedbookEntrySearchViewModel()
            {
                SearchViewModel = new RedbookEntrySearchPartialViewModel()
                {
                    LocationSelectList = locationList.ToSelectList(null, null, null, true, "Any", "Any"),
                    WeatherSelectListAM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(null, true, "Any"),
                    WeatherSelectListPM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(null, true, "Any"),
                    ManagerSelectListAM = eq.GetManagersForLocation(locationList).ToSelectList("sAMAccountName", "FullName", null, true, "Any", string.Empty),
                    ManagerSelectListPM = eq.GetManagersForLocation(locationList).ToSelectList("sAMAccountName", "FullName", null, true, "Any", string.Empty),
                },

                EmployeeInfo = employee
            };

            return model;
        }

        public RedbookEntrySearchViewModel InitializeRedbookEntrySearchViewModel(RedbookEntrySearchViewModel model, bool isRegional, bool isDivisional, bool isAdmin)
        {
            List<string> locationList = GetLocationList(model.EmployeeInfo, isRegional, isDivisional, isAdmin);

            model.SearchViewModel.LocationSelectList = locationList.ToSelectList(null, null, model.SearchViewModel.SearchDTO.LocationId, true, "Any", "Any");

            model.SearchViewModel.WeatherSelectListAM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(model.SearchViewModel.SearchDTO.SelectedWeatherAM, true, "Any");
            model.SearchViewModel.WeatherSelectListPM = cq.GetDistinctListByCodeCategory("Weather").ToSelectList(model.SearchViewModel.SearchDTO.SelectedWeatherPM, true, "Any");
            model.SearchViewModel.ManagerSelectListAM = model.SearchViewModel.SearchDTO.LocationId == "Any" ? eq.GetManagersForLocation(locationList).ToSelectList("sAMAccountName", "FullName", model.SearchViewModel.SearchDTO.ManagerOnDutyAM, true, "Any", string.Empty)
                                                                                                                    : eq.GetManagersForLocation(model.SearchViewModel.SearchDTO.LocationId).ToSelectList("sAMAccountName", "FullName", model.SearchViewModel.SearchDTO.ManagerOnDutyAM, true, "Any", string.Empty);
            model.SearchViewModel.ManagerSelectListPM = model.SearchViewModel.SearchDTO.LocationId == "Any" ? eq.GetManagersForLocation(locationList).ToSelectList("sAMAccountName", "FullName", model.SearchViewModel.SearchDTO.ManagerOnDutyPM, true, "Any", string.Empty)
                                                                                                                    : eq.GetManagersForLocation(model.SearchViewModel.SearchDTO.LocationId).ToSelectList("sAMAccountName", "FullName", model.SearchViewModel.SearchDTO.ManagerOnDutyPM, true, "Any", string.Empty);

            return model;
        }

        public RedbookEntrySearchPartialViewModel FilterDropdownLists(EmployeeDTO employee, string lId, string mAM, string mPM, bool isRegional, bool isDivisional, bool isAdmin)
        {
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin);

            RedbookEntrySearchPartialViewModel model = new RedbookEntrySearchPartialViewModel()
            {
                SearchDTO = new RedbookSearchDTO(lId, mAM, mPM),
                LocationSelectList = locationList.ToSelectList(null, null, lId, true, "Any", "Any"),
                ManagerSelectListAM = lId == "Any" ? eq.GetManagersForLocation(locationList).ToSelectList("sAMAccountName", "FullName", mAM, true, "Any", string.Empty)
                                                    : eq.GetManagersForLocation(lId).ToSelectList("sAMAccountName", "FullName", mAM, true, "Any", string.Empty),
                ManagerSelectListPM = lId == "Any" ? eq.GetManagersForLocation(locationList).ToSelectList("sAMAccountName", "FullName", mPM, true, "Any", string.Empty)
                                                    : eq.GetManagersForLocation(lId).ToSelectList("sAMAccountName", "FullName", mPM, true, "Any", string.Empty)
            };

            return model;
        }
    }

    public class SalesForecastInitializer
    {
        private readonly EmployeeQueries eq;
        private readonly BudgetQueries bq;
        private readonly SalesForecastQueries sfq;
        private readonly CodeQueries cq;

        public SalesForecastInitializer(SalesForecastQueries sfq, BudgetQueries bq, EmployeeQueries eq, CodeQueries cq)
        {
            this.sfq = sfq;
            this.bq = bq;
            this.eq = eq;
            this.cq = cq;
        }

        protected List<string> GetLocationList(EmployeeDTO employee, bool isRegional, bool isDivisional, bool isAdmin)
        {
            return isRegional ? eq.GetStoreLocationListByRegion(employee)
                                        : isDivisional ? eq.GetStoreLocationListByDivision(employee)
                                        : isAdmin ? eq.GetStoreLocationListForAdmin()
                                        : new List<string>();
        }

        protected List<DateTime> GetCurrentWeek(DateTime selectedDay)
        {
            int currentDayOfWeek = (int)selectedDay.DayOfWeek;
            DateTime sunday = selectedDay.AddDays(-currentDayOfWeek);
            DateTime monday = sunday.AddDays(1);

            if (currentDayOfWeek == 0)
            {
                monday = monday.AddDays(-7);
            }
            var dates = Enumerable.Range(0, 7).Select(days => monday.AddDays(days)).ToList();

            return dates;
        }

        protected List<SalesForecastDTO> GetSpecificWeekAsSalesForecastDTOList(DateTime selectedDay, string storeNumber)
        {
            List<SalesForecastDTO> theList = new List<SalesForecastDTO>();

            var dates = GetCurrentWeek(selectedDay);

            foreach (var day in dates)
            {
                if (!sfq.CheckForExistingSalesForecastByDate(day, storeNumber))
                    theList.Add(new SalesForecastDTO(day, sfq.fdq.GetSalesPriorYear(storeNumber, day), sfq.fdq.GetSalesPriorTwoYears(storeNumber, day), sfq.fdq.GetSalesPriorThreeYears(storeNumber, day), sfq.fdq.GetAverageSalesPerMonth(storeNumber, day), sfq.fdq.GetLaborForecast(storeNumber, day)));
                else
                    theList.Add(new SalesForecastDTO(sfq.GetSalesForecastsByDate(day, storeNumber)));
            }

            return theList;
        }

        protected decimal CalculateRecommendedLabor(BudgetDTO dto, decimal forecastAmountTotal, List<string> validLocations, string employeeStore)
        {
            if (validLocations.Contains(employeeStore) && dto.Budget.SalesBudgetAmount != 0 && dto.NumberOfWeeks != 0)
            {
                return ((decimal)dto.Budget.LaborBudgetAmount / dto.NumberOfWeeks) +
                                                ((forecastAmountTotal - ((decimal)dto.Budget.SalesBudgetAmount / dto.NumberOfWeeks))
                                                *
                                                (((decimal)dto.Budget.LaborBudgetAmount / (decimal)dto.Budget.SalesBudgetAmount) / 2));
            }
            else
            {
                return new decimal(-1);
            }
        }

        protected decimal CalculateRecommendedFOHLabor(FY18BudgetDTO dto, decimal recommendedLabor, List<string> validLocations, string employeeStore)
        {
            if (validLocations.Contains(employeeStore) && recommendedLabor != -1)
            {
                decimal numerator = dto.Account60205 + dto.Account60206;
                decimal denominator = dto.Account60205 + dto.Account60206 + dto.Account60210 + dto.Account60211;

                if (numerator == 0)
                    return new decimal(-1);
                else
                {
                    return recommendedLabor * (numerator / denominator);
                }
            }
            else
            {
                return new decimal(-1);
            }
        }

        protected decimal CalculateRecommendedBOHLabor(FY18BudgetDTO dto, decimal reccommendedLabor, List<string> validLocations, string employeeStore)
        {
            if (validLocations.Contains(employeeStore) && reccommendedLabor != -1)
            {
                decimal numerator = dto.Account60210 + dto.Account60211;
                decimal denominator = dto.Account60205 + dto.Account60206 + dto.Account60210 + dto.Account60211;

                if (numerator == 0)
                    return new decimal(-1);
                else
                {
                    return reccommendedLabor * (numerator / denominator);
                }
            }
            else
            {
                return new decimal(-1);
            }
        }

        protected SalesForecastCalculationDTO GetSalesForecastCalculationDTO(List<SalesForecastDTO> weekdays, string storeNumber)
        {
            List<string> validLocations = eq.GetAllValidStoreLocations();
            DateTime now = DateTime.Now.ToLocalTime();
            DateTime thursday = weekdays.Where(w => w.DayOfWeek == "Thursday").FirstOrDefault().DateOfEntry;

            SalesForecastColumnTotalsDTO columnTotalsDTO = new SalesForecastColumnTotalsDTO(weekdays);
            BudgetDTO budgetDTO = bq.GetBudgetByDate(thursday, storeNumber);
            FY18BudgetDTO fy18dto = new FY18BudgetDTO(bq.GetFY18Budgets(storeNumber), now);

            decimal recommendedLabor = CalculateRecommendedLabor(budgetDTO, columnTotalsDTO.ForecastAmountTotal, validLocations, storeNumber);
            decimal recommendedFOH = CalculateRecommendedFOHLabor(fy18dto, recommendedLabor, validLocations, storeNumber);
            decimal recommendedBOH = CalculateRecommendedBOHLabor(fy18dto, recommendedLabor, validLocations, storeNumber);

            SalesForecastCalculationDTO dto = new SalesForecastCalculationDTO(weekdays)
            {
                RecommendedLabor = recommendedLabor,
                Variance = columnTotalsDTO.LaborForecastTotal - recommendedLabor,

                RecommendedFOHLabor = recommendedFOH,
                VarianceFOH = weekdays.Sum(w => w.LaborFOH) - recommendedFOH,

                RecommendedBOHLabor = recommendedBOH,
                VarianceBOH = weekdays.Sum(w => w.LaborBOH) - recommendedBOH
            };

            return dto;
        }

        public SalesForecastViewModel InitializeSalesForecastEntryViewModel(string username, string selectedDate = "")
        {
            DateTime now = DateTime.Now.ToLocalTime();
            DateTime today = DateTime.Today.ToLocalTime();
            DateTime startDate = GetCurrentWeek(now).First();
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            DateTime.TryParse(selectedDate, out DateTime convertedSelectedDate);

            List<SalesForecastDTO> weekdays = GetSpecificWeekAsSalesForecastDTOList(string.IsNullOrEmpty(selectedDate) ? today : convertedSelectedDate, employee.StoreNumber);
            //auto refresh
            sfq.RefreshSalesForecastData(weekdays, employee.StoreNumber, username);

            SalesForecastViewModel model = new SalesForecastViewModel()
            {
                Weekdays = weekdays,
                AccessTime = now,
                StartDate = startDate,
                EndDate = startDate.AddDays(42),
                EndingPeriod = weekdays.Last().DateOfEntry,
                SelectedDateString = string.IsNullOrEmpty(selectedDate) ? today.ToShortDateString() : selectedDate,

                EmployeeInfo = employee,
                TicketURL = ConfigurationManager.AppSettings["SalesForecastTicketURL"],

                Calculations = GetSalesForecastCalculationDTO(weekdays, employee.StoreNumber)
            };

            return model;
        }

        public SalesForecastDetailPartialViewModel InitializeSalesForecastDetailPartialViewModel(string username, string selectedDate, string storeNumber)
        {
            DateTime now = DateTime.Now.ToLocalTime();
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            DateTime.TryParse(selectedDate, out DateTime convertedSelectedDate);

            List<SalesForecastDTO> weekdays = GetSpecificWeekAsSalesForecastDTOList(convertedSelectedDate, storeNumber);

            SalesForecastDetailPartialViewModel model = new SalesForecastDetailPartialViewModel()
            {
                SalesForecastDTO = weekdays.Where(w => w.DateOfEntry == convertedSelectedDate).FirstOrDefault(),
                Weekdays = weekdays,
                Calculations = GetSalesForecastCalculationDTO(weekdays, storeNumber)
            };

            return model;
        }

        public SalesForecastExportDTO GetSalesForecastExportDTO(string username, string selectedDate)
        {
            DateTime now = DateTime.Now.ToLocalTime();
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            DateTime.TryParse(selectedDate, out DateTime convertedSelectedDate);

            List<SalesForecastDTO> weekdays = GetSpecificWeekAsSalesForecastDTOList(convertedSelectedDate, employee.StoreNumber);

            SalesForecastExportDTO dto = new SalesForecastExportDTO()
            {
                Record = weekdays.Where(w => w.DateOfEntry == convertedSelectedDate).FirstOrDefault(),
                Weekdays = weekdays,
                Calculations = GetSalesForecastCalculationDTO(weekdays, employee.StoreNumber)
            };

            return dto;
        }

        public List<SalesForecastSummaryDTO> GetSalesForecastSummaryList(DateTime selectedDate, List<string> locationList)
        {
            List<SalesForecastSummaryDTO> summaryList = new List<SalesForecastSummaryDTO>();

            foreach (string location in locationList)
            {
                summaryList.Add(new SalesForecastSummaryDTO(location, GetSpecificWeekAsSalesForecastDTOList(selectedDate, location)));
            }

            return summaryList;
        }

        public List<SalesForecastSummaryColumnDTO> GetWeeklyReportColumnTotals(DateTime selectedDay)
        {
            List<DateTime> dates = GetCurrentWeek(selectedDay);
            List<SalesForecast> theList = sfq.GetSalesForecastByDates(dates);

            List<SalesForecastSummaryColumnDTO> columnSums = new List<SalesForecastSummaryColumnDTO>();

            foreach (var day in dates)
            {
                columnSums.Add(new SalesForecastSummaryColumnDTO(day, theList.Where(tl => tl.BusinessDate == day).ToList()));
            }

            return columnSums;
        }

        public SalesForecastReportViewModel InitializeSalesForecastReportViewModel()
        {
            DateTime now = DateTime.Now.ToLocalTime();
            List<DateTime> theWeek = GetCurrentWeek(now);

            SalesForecastReportViewModel model = new SalesForecastReportViewModel()
            {
                CurrentDate = now,
                SearchDTO = new SalesForecastSummarySearchDTO(now),
                SummaryList = GetSalesForecastSummaryList(now, eq.GetLocationList()),
                ColumnTotalList = GetWeeklyReportColumnTotals(now),
                EndingPeriod = theWeek.LastOrDefault(),
                StartingPeriod = theWeek.FirstOrDefault()
            };

            return model;
        }

        public SalesForecastReportViewModel InitializeSalesForecastReportViewModel(SalesForecastReportViewModel model)
        {
            DateTime desiredDate = model.SearchDTO.DesiredDate;

            DateTime now = DateTime.Now.ToLocalTime();
            List<DateTime> theWeek = GetCurrentWeek(desiredDate);

            model.CurrentDate = now;
            model.SearchDTO = new SalesForecastSummarySearchDTO(desiredDate);
            model.SummaryList = GetSalesForecastSummaryList(desiredDate, eq.GetLocationList());
            model.ColumnTotalList = GetWeeklyReportColumnTotals(desiredDate);
            model.EndingPeriod = theWeek.LastOrDefault();
            model.StartingPeriod = theWeek.FirstOrDefault();

            return model;
        }

        public SalesForecastSearchViewModel InitializeSalesForecastSearchViewModel(string username, bool isRegional, bool isDivisional, bool isAdmin)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin);

            SalesForecastSearchViewModel model = new SalesForecastSearchViewModel()
            {
                SearchViewModel = new SalesForecastSearchPartialViewModel()
                {
                    LocationSelectList = locationList.ToSelectList(null, null, null, true, "Any", "Any"),
                    //WeekdaySelectList = DomainConstants.WeekdayConstants.WeekdayList().ToSelectList(null, null, null, true, "Any", "Any")
                },

                EmployeeInfo = employee
            };

            return model;
        }

        public SalesForecastSearchViewModel InitializeSalesForecastSearchViewModel(SalesForecastSearchDTO searchDTO, string username, bool isRegional, bool isDivisional, bool isAdmin)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin).Select(l => l.Substring(0, 3)).ToList();
            SalesForecast salesForecast = sfq.GetSalesForecastRecordsByDate(searchDTO.EndDate, searchDTO.LocationId);

            SalesForecastSearchViewModel model = new SalesForecastSearchViewModel()
            {
                SearchViewModel = new SalesForecastSearchPartialViewModel(salesForecast, searchDTO.StartDate, searchDTO.EndDate)
                {
                    LocationSelectList = locationList.ToSelectList(null, null, searchDTO.LocationId, true, "Any", "Any"),
                },

                EmployeeInfo = employee
            };

            model.SearchResults = CreateSearchResultDTOs(sfq.GetSalesForecastEntries(model.SearchViewModel.SearchDTO, locationList, searchDTO.StartDate, searchDTO.EndDate));

            return model;
        }

        protected List<SalesForecastSearchResultDTO> CreateSearchResultDTOs(List<SalesForecast> searchResults)
        {
            List<SalesForecastSearchResultDTO> results = new List<SalesForecastSearchResultDTO>();
            var test1 = searchResults.GroupBy(x => x.StoreNumber);

            foreach (var store in searchResults.GroupBy(x => x.StoreNumber))
            {
                var storeByWeeks = store.GroupBy(x => CultureInfo.CurrentCulture.DateTimeFormat.Calendar
                                                    .GetWeekOfYear(x.BusinessDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday)).ToList();

                foreach(var storeByWeek in storeByWeeks)
                {
                    List<SalesForecast> salesForecasts = storeByWeek.ToList();
                    List<SalesForecastDTO> salesForecastDTOs = new List<SalesForecastDTO>();

                    foreach (var sf in salesForecasts)
                    {
                        salesForecastDTOs.Add(new SalesForecastDTO(sf));                        
                    }

                    var resultDTO = new SalesForecastSearchResultDTO(salesForecasts)
                    {
                        Calculations = GetSalesForecastCalculationDTO(salesForecastDTOs, salesForecasts.FirstOrDefault().StoreNumber)
                    };

                    results.Add(resultDTO);
                }
            }

            return results;
        }

        public SalesForecastSearchViewModel InitializeSalesForecastSearchViewModel(SalesForecastSearchViewModel model, bool isRegional, bool isDivisional, bool isAdmin)
        {
            List<string> locationList = GetLocationList(model.EmployeeInfo, false, false, isAdmin).Select(l => l.Substring(0, 3)).ToList();

            DateTime fiscStart = model.SearchViewModel.SearchDTO.StartDate = GetCurrentWeek(model.SearchViewModel.SearchDTO.StartDate).FirstOrDefault();
            DateTime fiscEnd = model.SearchViewModel.SearchDTO.EndDate = GetCurrentWeek(model.SearchViewModel.SearchDTO.EndDate).LastOrDefault();

            model.SearchViewModel.LocationSelectList = locationList.ToSelectList(null, null, model.SearchViewModel.SearchDTO.LocationId, true, "Any", "Any");
            //model.SearchViewModel.WeekdaySelectList = DomainConstants.WeekdayConstants.WeekdayList().ToSelectList(null, null, model.SearchViewModel.SearchDTO.DayOfWeek, true, "Any", "Any");

            List<SalesForecast> queryResults = sfq.GetSalesForecastEntries(model.SearchViewModel.SearchDTO, locationList, fiscStart, fiscEnd);

            model.SearchResults = CreateSearchResultDTOs(queryResults);
            //model.SearchResults = sfq.GetSalesForecastEntries(model.SearchViewModel.SearchDTO, locationList.Select(l => l.Substring(0, 3)).ToList(), fiscStart, fiscEnd);

            return model;
        }


        //public SalesForecastCreateEditPartialViewModel InitializeSalesForecastEditViewModel(int id, string username)
        //{
        //    SalesForecast salesForecast = sfq.FindById(id);
        //    EmployeeDTO employee = eq.GetEmployeeInfo(username);
        //    List<SalesForecastDTO> weekdays = GetSpecificWeekAsSalesForecastDTOList(salesForecast.BusinessDate, employee.StoreNumber);

        //    SalesForecastCreateEditPartialViewModel model = new SalesForecastCreateEditPartialViewModel()
        //    {
        //        SalesForecast = salesForecast,
        //        Calculations = GetSalesForecastCalculationDTO(weekdays, employee)
        //    };

        //    return model;
        //}

        public SalesForecastEditDetailPartialViewModel InitializeSalesForecastEditDetailPartialViewModel(string weekEnding, string storeNumber, string username)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            DateTime.TryParse(weekEnding, out DateTime convertedWeekEnding);

            List<SalesForecastDTO> weekdays = GetSpecificWeekAsSalesForecastDTOList(convertedWeekEnding, storeNumber);

            SalesForecastEditDetailPartialViewModel model = new SalesForecastEditDetailPartialViewModel()
            {
                Weekdays = weekdays,
                Calculations = GetSalesForecastCalculationDTO(weekdays, storeNumber),
                StoreNumber = storeNumber
            };

            return model;
        }
    }

    public class TipReportingInitializer
    {
        private readonly EmployeeQueries eq;
        private readonly TipQueries tq;

        public TipReportingInitializer(EmployeeQueries eq, TipQueries tq)
        {
            this.eq = eq;
            this.tq = tq;
        }

        protected List<string> GetLocationList(EmployeeDTO employee, bool isRegional, bool isDivisional, bool isAdmin)
        {
            return isRegional ? eq.GetStoreLocationListByRegion(employee)
                                        : isDivisional ? eq.GetStoreLocationListByDivision(employee)
                                        : isAdmin ? eq.GetStoreLocationListForAdmin()
                                        : new List<string>();
        }

        protected List<DateTime> GetCurrentWeek(DateTime selectedDay)
        {
            int currentDayOfWeek = (int)selectedDay.DayOfWeek;
            DateTime sunday = selectedDay.AddDays(-currentDayOfWeek);
            DateTime monday = sunday.AddDays(1);

            if (currentDayOfWeek == 0)
            {
                monday = monday.AddDays(-7);
            }
            var dates = Enumerable.Range(0, 7).Select(days => monday.AddDays(days)).ToList();

            return dates;
        }

        public TipReportingViewModel InitializeTipReportingViewModel(string username, bool isRegional, bool isDivisional, bool isAdmin, bool isLastWeek = false)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);

            List<DateTime> daysInWeek = GetCurrentWeek(!isLastWeek ? DateTime.Today : DateTime.Today.AddDays(-7));

            TipReportingViewModel model = new TipReportingViewModel()
            {
                EmployeeInfo = employee,
                MakeUpPayList = tq.GetOutstandingMakeUps(employee.StoreNumber, daysInWeek.LastOrDefault()),
                AccessTime = DateTime.Now,
                EndingPeriod = daysInWeek.LastOrDefault(),
                CurrentWeekFlag = !isLastWeek
            };

            return model;
        }

        public TipReportingSearchViewModel InitializeTipReportingSearchViewModel(string username, bool isRegional, bool isDivisional, bool isAdmin)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin);

            TipReportingSearchViewModel model = new TipReportingSearchViewModel()
            {
                EmployeeInfo = employee,
                SearchResults = new List<MakeUpPay>(),
                LocationSelectList = locationList.ToSelectList(null, null, null, true, "Any", "Any"),
                SearchDTO = new TipReportingSearchDTO()
            };

            return model;
        }

        public TipReportingSearchViewModel InitializeTipReportingSearchViewModel(TipReportingSearchDTO searchDTO, string username, bool isRegional, bool isDivisional, bool isAdmin)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin);

            DateTime fiscStart = searchDTO.StartDate = GetCurrentWeek(searchDTO.StartDate).LastOrDefault();
            DateTime fiscEnd = searchDTO.EndDate = GetCurrentWeek(searchDTO.EndDate).LastOrDefault();

            TipReportingSearchViewModel model = new TipReportingSearchViewModel()
            {
                EmployeeInfo = employee,
                SearchResults = tq.GetOutstandingMakeUps(searchDTO, locationList.Select(l => l.Substring(0, 3)).ToList(), fiscStart, fiscEnd),
                LocationSelectList = locationList.ToSelectList(null, null, searchDTO.SelectedLocation, true, "Any", "Any"),
                SearchDTO = searchDTO
            };

            return model;
        }
    }

    public class SpreadHoursInitializer
    {
        private readonly EmployeeQueries eq;
        private readonly SpreadHourQueries shq;

        public SpreadHoursInitializer(EmployeeQueries eq, SpreadHourQueries shq)
        {
            this.eq = eq;
            this.shq = shq;
        }

        protected List<string> GetLocationList(EmployeeDTO employee, bool isRegional, bool isDivisional, bool isAdmin)
        {
            return isRegional ? eq.GetStoreLocationListByRegion(employee)
                                        : isDivisional ? eq.GetStoreLocationListByDivision(employee)
                                        : isAdmin ? eq.GetStoreLocationListForAdmin()
                                        : new List<string>();
        }

        protected List<DateTime> GetCurrentWeek(DateTime selectedDay)
        {
            int currentDayOfWeek = (int)selectedDay.DayOfWeek;
            DateTime sunday = selectedDay.AddDays(-currentDayOfWeek);
            DateTime monday = sunday.AddDays(1);

            if (currentDayOfWeek == 0)
            {
                monday = monday.AddDays(-7);
            }
            var dates = Enumerable.Range(0, 7).Select(days => monday.AddDays(days)).ToList();

            return dates;
        }

        protected List<SpreadHourDTO> GetSpreadHourDTOs(List<SpreadHour> spreadHours)
        {
            List<SpreadHourDTO> dtoList = new List<SpreadHourDTO>();
            List<MinimumWage> minimumWages = shq.GetMinimumWages();

            foreach (SpreadHour sp in spreadHours)
            {
                MinimumWage minWage = new MinimumWage();

                if (minimumWages.Any(mw => mw.StoreNumber == sp.StoreNumber))
                    minWage = minimumWages.Where(mw => mw.StoreNumber == sp.StoreNumber).FirstOrDefault();

                dtoList.Add(new SpreadHourDTO(sp, minWage));
            }

            return dtoList;
        }

        public SpreadHourViewModel InitializeSpreadHourViewModel(string username, bool isLastWeek = false)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);

            List<DateTime> daysInWeek = GetCurrentWeek(!isLastWeek ? DateTime.Today : DateTime.Today.AddDays(-7));

            SpreadHourViewModel model = new SpreadHourViewModel()
            {
                EmployeeInfo = employee,
                SpreadHours = GetSpreadHourDTOs(shq.GetSpreadHoursByWeek(employee.StoreNumber, daysInWeek.FirstOrDefault(), daysInWeek.LastOrDefault())),
                AccessTime = DateTime.Now,
                EndingPeriod = daysInWeek.LastOrDefault(),
                CurrentWeekFlag = !isLastWeek
            };

            return model;
        }

        public SpreadHourSearchViewModel InitializeSpreadHourSearchViewModel(string username, bool isRegional, bool isDivisional, bool isAdmin)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin);

            SpreadHourSearchViewModel model = new SpreadHourSearchViewModel()
            {
                LocationSelectList = locationList.ToSelectList(null, null, null, true, "Any", "Any"),
                SearchDTO = new SpreadHourSearchDTO(),
                SearchResults = new List<SpreadHourDTO>(),
                EmployeeInfo = employee
            };

            return model;
        }


        public SpreadHourSearchViewModel InitializeSpreadHourSearchViewModel(SpreadHourSearchDTO searchDTO, string username, bool isRegional, bool isDivisional, bool isAdmin)
        {
            EmployeeDTO employee = eq.GetEmployeeInfo(username);
            List<string> locationList = GetLocationList(employee, isRegional, isDivisional, isAdmin);

            DateTime fiscStart = searchDTO.StartDate = GetCurrentWeek(searchDTO.StartDate).FirstOrDefault();
            DateTime fiscEnd = searchDTO.EndDate = GetCurrentWeek(searchDTO.EndDate).LastOrDefault();

            SpreadHourSearchViewModel model = new SpreadHourSearchViewModel()
            {
                EmployeeInfo = employee,
                SearchResults = GetSpreadHourDTOs(shq.GetSpreadHours(searchDTO, locationList.Select(l => l.Substring(0, 3)).ToList(), fiscStart, fiscEnd)),
                LocationSelectList = locationList.ToSelectList(null, null, searchDTO.SelectedLocation, true, "Any", "Any"),
                SearchDTO = searchDTO
            };

            return model;
        }
    }
}