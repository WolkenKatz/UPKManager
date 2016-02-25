﻿using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows;

using AutoMapper;

using STR.Common.Extensions;
using STR.MvvmCommon.Contracts;
using STR.MvvmCommon.Mef;

using UpkManager.Domain.Contracts;


namespace UpkManager.Wpf {

  public partial class App : Application {

    #region Private Fields

    private readonly IMvvmContainer container;

    #endregion Private Fields

    #region Constructor

    public App() {
      container = new MvvmContainer();

      container.Initialize(() => new AggregateCatalog(new DirectoryCatalog(Directory.GetCurrentDirectory(), "UpkManager.Wpf.exe"),
                                                      new DirectoryCatalog(Directory.GetCurrentDirectory(), "UpkManager.*.dll"),
                                                      new DirectoryCatalog(Directory.GetCurrentDirectory(), "STR.*.dll")));
    }

    #endregion Constructor

    #region Overrides

    protected override void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      IEnumerable<IAutoMapperConfiguration> configurations = container.GetAll<IAutoMapperConfiguration>();

      MapperConfiguration mapperConfiguration = new MapperConfiguration(cfg => configurations.ForEach(configuration => configuration.RegisterMappings(cfg)));

      mapperConfiguration.AssertConfigurationIsValid();

      container.RegisterInstance(mapperConfiguration.CreateMapper());

      container.GetAll<IController>();
    }

    #endregion Overrides

  }

}
