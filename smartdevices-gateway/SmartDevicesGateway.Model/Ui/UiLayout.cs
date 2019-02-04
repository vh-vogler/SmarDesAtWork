﻿//
// Copyright (c) Vogler Engineering GmbH. All rights reserved.
// Licensed under the MIT License. See LICENSE.md in the project root for license information.
//
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SmartDevicesGateway.Model.Ui
{
    public class UiLayout : UiComponent
    {
        public string Title { get; set; }

        public ICollection<UiComponent> Elements { get; set; }
    }
}
