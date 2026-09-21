using Microsoft.Maui.Controls;
using System;

namespace PassGold.Views;

public partial class PhotoViewerPage : ContentPage
{
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset = 0;
    private double _yOffset = 0;

    public PhotoViewerPage(string photoUrl)
    {
        InitializeComponent();

        PhotoImage.Source = photoUrl;

        // Oculta el spinner apenas la imagen termina de cargar (o si falla, igual
        // lo oculta para no dejarlo girando para siempre).
        PhotoImage.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Image.IsLoading) && !PhotoImage.IsLoading)
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        };
    }

    private async void CerrarBtn_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        // Boton atras fisico de Android tambien cierra el visor.
        _ = Navigation.PopModalAsync();
        return true;
    }

    // Doble toque: si ya esta con zoom, vuelve a tamano normal; si esta en
    // tamano normal, hace un zoom rapido x2 - el gesto clasico de galeria/WhatsApp.
    private void OnDoubleTapped(object sender, EventArgs e)
    {
        if (_currentScale > 1)
        {
            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;
            PhotoImage.Scale = 1;
            PhotoImage.TranslationX = 0;
            PhotoImage.TranslationY = 0;
        }
        else
        {
            _currentScale = 2;
            PhotoImage.Scale = 2;
        }
    }

    // Pinch-to-zoom: receta estandar de MAUI, con centro de zoom en el punto
    // donde el usuario apoya los dedos (no siempre en el centro de la imagen).
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _startScale = PhotoImage.Scale;
            PhotoImage.AnchorX = 0;
            PhotoImage.AnchorY = 0;
        }

        if (e.Status == GestureStatus.Running)
        {
            _currentScale += (e.Scale - 1) * _startScale;
            _currentScale = Math.Max(1, Math.Min(4, _currentScale));

            double renderedX = PhotoImage.X + _xOffset;
            double deltaX = renderedX / Width;
            double deltaWidth = Width / (PhotoImage.Width * _startScale);
            double originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

            double renderedY = PhotoImage.Y + _yOffset;
            double deltaY = renderedY / Height;
            double deltaHeight = Height / (PhotoImage.Height * _startScale);
            double originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

            double targetX = _xOffset - (originX * PhotoImage.Width) * (_currentScale - _startScale);
            double targetY = _yOffset - (originY * PhotoImage.Height) * (_currentScale - _startScale);

            PhotoImage.TranslationX = Clamp(targetX, -Width * (_currentScale - 1), 0);
            PhotoImage.TranslationY = Clamp(targetY, -Height * (_currentScale - 1), 0);

            PhotoImage.Scale = _currentScale;
        }

        if (e.Status == GestureStatus.Completed)
        {
            _xOffset = PhotoImage.TranslationX;
            _yOffset = PhotoImage.TranslationY;
        }
    }

    //  solo mueve la imagen si ya esta con zoom aplicado (si no, no tiene
    // sentido arrastrar una imagen a tamano normal).
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (_currentScale <= 1) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                PhotoImage.TranslationX = Clamp(_xOffset + e.TotalX, -Width * (_currentScale - 1), 0);
                PhotoImage.TranslationY = Clamp(_yOffset + e.TotalY, -Height * (_currentScale - 1), 0);
                break;

            case GestureStatus.Completed:
                _xOffset = PhotoImage.TranslationX;
                _yOffset = PhotoImage.TranslationY;
                break;
        }
    }

    private static double Clamp(double value, double min, double max)
    {
        if (min > max) (min, max) = (max, min);
        return Math.Max(min, Math.Min(max, value));
    }
}