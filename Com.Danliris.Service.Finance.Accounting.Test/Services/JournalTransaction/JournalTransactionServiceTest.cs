﻿using Com.Danliris.Service.Finance.Accounting.Lib;
using Com.Danliris.Service.Finance.Accounting.Lib.BusinessLogic.Services.JournalTransaction;
using Com.Danliris.Service.Finance.Accounting.Lib.BusinessLogic.Services.Master;
using Com.Danliris.Service.Finance.Accounting.Lib.Models.JournalTransaction;
using Com.Danliris.Service.Finance.Accounting.Lib.Models.MasterCOA;
using Com.Danliris.Service.Finance.Accounting.Lib.Services.HttpClientService;
using Com.Danliris.Service.Finance.Accounting.Lib.Services.IdentityService;
using Com.Danliris.Service.Finance.Accounting.Lib.Utilities;
using Com.Danliris.Service.Finance.Accounting.Lib.ViewModels.JournalTransaction;
using Com.Danliris.Service.Finance.Accounting.Test.DataUtils.JournalTransaction;
using Com.Danliris.Service.Finance.Accounting.Test.DataUtils.Masters.COADataUtils;
using Com.Danliris.Service.Finance.Accounting.Test.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Com.Danliris.Service.Finance.Accounting.Test.Services.JournalTransaction
{
    public class JournalTransactionServiceTest
    {
        private const string ENTITY = "JournalTransaction";
        //private PurchasingDocumentAcceptanceDataUtil pdaDataUtil;
        //private readonly IIdentityService identityService;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            return string.Concat(sf.GetMethod().Name, "_", ENTITY);
        }

        private FinanceDbContext _dbContext(string testName)
        {
            DbContextOptionsBuilder<FinanceDbContext> optionsBuilder = new DbContextOptionsBuilder<FinanceDbContext>();
            optionsBuilder
                .UseInMemoryDatabase(testName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            FinanceDbContext dbContext = new FinanceDbContext(optionsBuilder.Options);

            return dbContext;
        }

        private JournalTransactionDataUtil _dataUtil(JournalTransactionService service)
        {
            var coaService = new COAService(GetServiceProvider().Object, service._DbContext);
            var coaDataUtil = new COADataUtil(coaService);
            return new JournalTransactionDataUtil(service, coaDataUtil);
        }

        private Mock<IServiceProvider> GetServiceProvider()
        {
            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider
                .Setup(x => x.GetService(typeof(IHttpClientService)))
                .Returns(new JournalHttpClientTestService());

            serviceProvider
                .Setup(x => x.GetService(typeof(IIdentityService)))
                .Returns(new IdentityService() { Token = "Token", Username = "Test", TimezoneOffset = 7 });


            return serviceProvider;
        }

        [Fact]
        public async Task Should_Success_Get_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var data = await _dataUtil(service).GetTestData();
            var Response = service.Read(1, 25, "{}", null, data.DocumentNo, "{}");
            Assert.NotEmpty(Response.Data);
        }

        [Fact]
        public async Task Should_Success_Get_Data_By_Id()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var model = await _dataUtil(service).GetTestData();
            var Response = await service.ReadByIdAsync(model.Id);
            Assert.NotNull(Response);
        }

        [Fact]
        public async Task Should_Success_Create_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var model = _dataUtil(service).GetNewData();
            var Response = await service.CreateAsync(model);
            Assert.NotEqual(0, Response);
        }

        [Fact]
        public void Should_No_Error_Validate_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var vm = _dataUtil(service).GetDataToValidate();

            Assert.True(vm.Validate(null).Count() == 0);
        }

        [Fact]
        public void Should_Success_Validate_All_Null_Data()
        {
            var vm = new JournalTransactionViewModel();

            Assert.True(vm.Validate(null).Count() > 0);
        }

        [Fact]
        public async Task Should_Success_Update_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var model = await _dataUtil(service).GetTestData();
            var newModel = await service.ReadByIdAsync(model.Id);
            newModel.Description = "NewDescription";
            var Response = await service.UpdateAsync(newModel.Id, newModel);
            Assert.NotEqual(0, Response);
        }

        [Fact]
        public async Task Should_Success_Delete_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var model = await _dataUtil(service).GetTestData();
            //var modelToDelete = await service.ReadByIdAsync(model.Id);

            var Response = await service.DeleteAsync(model.Id);
            Assert.NotEqual(0, Response);
        }

        [Fact]
        public void Should_Error_Validate_Data_NotEqual_Total()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var vm = _dataUtil(service).GetDataToValidate();
            vm.Items[1].Credit = 5000;
            vm.Date = DateTimeOffset.UtcNow.AddDays(2);

            Assert.True(vm.Validate(null).Count() > 0);
        }

        [Fact]
        public void Should_Error_Validate_Data_Debit_And_Credit_Exist()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var vm = _dataUtil(service).GetDataToValidate();
            vm.Items[1].Debit = 5000;
            vm.Items[1].Credit = 1000;
            vm.Items[1].COA = null;

            Assert.True(vm.Validate(null).Count() > 0);
        }

        [Fact]
        public void Should_Error_Validate_Data_Debit_And_Credit_EqualsZero()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var vm = _dataUtil(service).GetDataToValidate();
            vm.Items[1].Credit = 0;
            vm.Items[1].Credit = 0;

            Assert.True(vm.Validate(null).Count() > 0);
        }

        [Fact]
        public async Task Should_Success_Update_Data_NewItem()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var model = await _dataUtil(service).GetTestData();
            model.Description = "NewDescription";

            var newItemDebit = new JournalTransactionItemModel()
            {
                COA = new COAModel()
                {
                    Id = 1,
                    Code = ""
                },
                Debit = 100
            };
            model.Items.Add(newItemDebit);

            var newItemCredit = new JournalTransactionItemModel()
            {
                COA = new COAModel()
                {
                    Id = 1,
                    Code = ""
                },
                Credit = 100
            };
            model.Items.Add(newItemCredit);

            var Response = await service.UpdateAsync(model.Id, model);
            Assert.NotEqual(0, Response);
        }

        [Fact]
        public async Task Should_Success_Generate_Excel()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));

            var data = await _dataUtil(service).GetTestData();
            var reportResponse = service.GenerateExcel(data.Date.AddDays(-1), data.Date, 7);
            Assert.NotNull(reportResponse);
        }

        [Fact]
        public void Should_Success_Generate_Excel_Empty()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));

            var reportResponse = service.GenerateExcel(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 7);
            Assert.NotNull(reportResponse);
        }

        [Fact]
        public async Task Should_Success_Get_Report()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var data = await _dataUtil(service).GetTestData();

            var reportResponse = service.GetReport(1, 25, data.Date.AddDays(-1), data.Date, 7);
            Assert.NotNull(reportResponse.Item1);

            var reportResponse2 = service.GetReport(1, 25, data.Date.AddDays(-1), null, 7);
            Assert.NotNull(reportResponse2.Item1);

            var reportResponse3 = service.GetReport(1, 25, null, data.Date, 7);
            Assert.NotNull(reportResponse3.Item1);

            var reportResponse4 = service.GetReport(1, 25, null, null, 7);
            Assert.NotNull(reportResponse4.Item1);
        }

        [Fact]
        public async Task Should_Success_ReverseJournalTransaction_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var model = _dataUtil(service).GetNewData();
            var createdData = await service.CreateAsync(model);
            var response = await service.ReverseJournalTransactionByReferenceNo(model.ReferenceNo);
            Assert.NotEqual(0, response);
        }

        [Fact]
        public async Task Should_Error_CreateDuplicateReferenceNo_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            var model = _dataUtil(service).GetNewData();
            var createdData = await service.CreateAsync(model);

            var newModel = _dataUtil(service).GetNewData();
            newModel.ReferenceNo = model.ReferenceNo;
            //var response = service.ReverseJournalTransactionByReferenceNo(model.ReferenceNo).Result;
            await Assert.ThrowsAsync<ServiceValidationException>(() => service.CreateAsync(newModel));
        }

        [Fact]
        public async Task Should_Error_ReverseJournalTransaction_Data()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));
            await Assert.ThrowsAsync<Exception>(() => service.ReverseJournalTransactionByReferenceNo("test"));
        }

        [Fact]
        public async Task Should_Success_Generate_SubLedger_Excel()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));

            var data = await _dataUtil(service).GetTestData();
            var reportResponse = service.GetSubLedgerReportXls(data.Date.Month, data.Date.Year, data.Items.ToList()[0].COAId, 1);
            Assert.NotNull(reportResponse);
        }

        [Fact]
        public async Task Should_Success_Generate_SubLedger()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));

            var data = await _dataUtil(service).GetTestData();
            var reportResponse = service.GetSubLedgerReport(data.Date.Month, data.Date.Year, data.Items.ToList()[0].COAId, 1);
            Assert.NotNull(reportResponse);
        }

        [Fact]
        public async Task Should_Success_Posting_Transaction()
        {
            var service = new JournalTransactionService(GetServiceProvider().Object, _dbContext(GetCurrentMethod()));

            var data = await _dataUtil(service).GetTestPostedData();
            var reportResponse = await service.PostTransactionAsync(data.Id);
            Assert.NotEqual(0, reportResponse);
        }
    }
}

