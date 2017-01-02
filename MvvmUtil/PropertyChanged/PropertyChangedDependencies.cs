﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MvvmUtil.PropertyChanged
{
    public class PropertyChangedDependencies
    {
        private INotifyPropertyChanged NotifyInstance;
        private IRaisePropertyChanged RaiseInstance;

        private object RuleLock;
        private List<DependencyRule> Rules;

        public PropertyChangedDependencies(INotifyPropertyChanged notify, IRaisePropertyChanged raise)
        {
            this.RuleLock = new object();
            this.Rules = new List<DependencyRule>();
            this.NotifyInstance = notify;
            this.RaiseInstance = raise;
            this.NotifyInstance.PropertyChanged += this.HandlePropertyChanged;
        }

        public PropertyChangedDependencies(object instance)
            : this((INotifyPropertyChanged)instance, (IRaisePropertyChanged)instance) { }

        public void AddDependency(string property, string dependency)
        {
            lock (this.RuleLock)
            {
                this.Rules.Add(new DependencyRule(property, dependency));
                if (this.AnyLoop(property, dependency))
                {
                    throw new InvalidOperationException("Creating dependency loops is not allowed");
                }
            }
        }

        private bool AnyLoop(string property, string dependency)
        {
            if (property.Equals(dependency)) return true;
            return this.Rules
                .Where(rule => rule.Property.Equals(dependency))
                .Select(rule => rule.Dependency)
                .Any(dep => this.AnyLoop(property, dep));
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            lock (this.RuleLock)
            {
                this.Rules
                    .Where(dependency => dependency.Dependency.Equals(args.PropertyName))
                    .Select(dependency => dependency.Property)
                    .ToList()
                    .ForEach(this.RaisePropertyChanged);
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            this.RaiseInstance.RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class DependencyRule
    {
        public string Property { get; set; }
        public string Dependency { get; set; }

        public DependencyRule(string property, string dependency)
        {
            this.Property = property;
            this.Dependency = dependency;
        }
    }
}
