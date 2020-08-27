﻿using Com.Danliris.Service.Finance.Accounting.Lib.BusinessLogic.VBRequestDocument;
using Com.Danliris.Service.Finance.Accounting.Lib.Models.VBRequestDocument;
using System;
using System.Collections.Generic;

namespace Com.Danliris.Service.Finance.Accounting.Lib.BusinessLogic.VBRealizationDocument
{
    public class VBRealizationWithPODto
    {
        public VBRequestDocumentModel VBRequestDocument { get; set; }
        public DateTimeOffset Date { get; set; }
        public int Id { get; set; }
        public string Type { get; set; }
        public UnitDto SuppliantUnit { get; set; }
        public CurrencyDto Currency { get; set; }
        public List<VBRealizationWithPOItemDto> Items { get; internal set; }
    }
}