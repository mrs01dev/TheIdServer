﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aguacongas.TheIdServer.BlazorApp.Components
{
    public abstract class AutoCompleteModel<T> : ComponentBase, IDisposable
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public T Entity { get; set; }

        [Parameter]
        public EventCallback DeleteClicked { get; set; }

        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

        [Parameter]
        public Func<string, bool> Validate { get; set; }

        protected abstract bool IsReadOnly { get; }

        protected string Id { get; } = Guid.NewGuid().ToString();

        protected string SelectedValue { get; set; }

        protected IEnumerable<string> FilteredValues { get; private set; }

        private CancellationTokenSource _cancellationTokenSource;

        [JSInvokable]
        public Task EnterKeyPressed()
        {
            return SetSelectedValue(SelectedValue);
        }

        protected Task SetSelectedValue(string value)
        {
            if (Validate != null && !Validate.Invoke(value))
            {
                return Task.CompletedTask;
            }
            SetValue(value);
            FilteredValues = null;
            return ValueChanged.InvokeAsync(Entity);
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !IsReadOnly)
            {
                return JSRuntime.InvokeVoidAsync("browserInteropt.preventEnterKeyPress", Id, DotNetObjectReference.Create(this))
                    .AsTask();
            }
            return Task.CompletedTask;
        }

        protected Task Filter()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            return Task.Delay(250, token)
                    .ContinueWith(async task =>
                    {
                        if (task.IsCanceled)
                        {
                            return;
                        }
                        FilteredValues = await GetFilteredValues(SelectedValue);
                        if (FilteredValues.Any())
                        {
                            await JSRuntime.InvokeVoidAsync("bootstrapInteropt.showDropDownMenu", Id);
                        }
                        StateHasChanged();
                    }, TaskScheduler.Default);
        }

        protected Task OnInputChanged(ChangeEventArgs e)
        {
            SelectedValue = e.Value as string;
            return Filter();
        }

        protected abstract void SetValue(string inputValue);

        protected abstract Task<IEnumerable<string>> GetFilteredValues(string term);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}