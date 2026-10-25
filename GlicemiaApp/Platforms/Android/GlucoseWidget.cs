using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using GlicemiaApp.Models;
using GlicemiaApp.Services;

namespace GlicemiaApp;

/// <summary>
/// Widget da tela inicial: os últimos registros (hora, mg/dL, contexto) e o botão "+ Registrar".
/// Atenção: ele lê o banco sem PIN (veja WidgetFeed e SECURITY.md); a observação nunca é exibida.
/// </summary>
[BroadcastReceiver(Label = "Glicemia", Exported = true)]
[IntentFilter(["android.appwidget.action.APPWIDGET_UPDATE"])]
[MetaData("android.appwidget.provider", Resource = "@xml/glucose_widget_info")]
public class GlucoseWidget : AppWidgetProvider
{
    public const string ActionQuickAdd = "com.exemplo.glicemia.QUICK_ADD";
    public const string ExtraContext = "context";

    public override void OnUpdate(Context? context, AppWidgetManager? manager, int[]? widgetIds)
    {
        if (context is null || manager is null || widgetIds is null) return;

        // Mantém o receiver vivo enquanto o banco é lido.
        var pending = GoAsync();
        Task.Run(async () =>
        {
            try { await RenderAsync(context, manager, widgetIds); }
            finally { pending?.Finish(); }
        });
    }

    /// <summary>Redesenha todos os widgets colocados na tela inicial.</summary>
    public static Task RefreshAsync(Context context)
    {
        var manager = AppWidgetManager.GetInstance(context);
        var ids = manager?.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(GlucoseWidget))));
        return manager is null || ids is null || ids.Length == 0 ? Task.CompletedTask : RenderAsync(context, manager, ids);
    }

    static async Task RenderAsync(Context context, AppWidgetManager manager, int[] widgetIds)
    {
        List<GlucoseReading> rows;
        try
        {
            var feed = IPlatformApplication.Current?.Services.GetService(typeof(WidgetFeed)) as WidgetFeed;
            rows = feed is null ? [] : await feed.LatestAsync();
        }
        catch (Exception)
        {
            rows = []; // banco sendo fechado por um bloqueio ao mesmo tempo; a próxima atualização corrige
        }

        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_glucose);

        var open = Open(context);
        views.SetOnClickPendingIntent(Resource.Id.widget_root, open);
        views.SetOnClickPendingIntent(Resource.Id.widget_add, open);

        views.RemoveAllViews(Resource.Id.widget_rows);
        views.SetViewVisibility(Resource.Id.widget_empty, rows.Count == 0 ? ViewStates.Visible : ViewStates.Gone);
        foreach (var r in rows)
        {
            var row = new RemoteViews(context.PackageName, Resource.Layout.widget_row);
            row.SetTextViewText(Resource.Id.row_time, r.MeasuredAtUtc.ToLocalTime().ToString("dd/MM HH:mm"));
            row.SetTextViewText(Resource.Id.row_value, $"{r.ValueMgDl} mg/dL");
            row.SetTextViewText(Resource.Id.row_context, r.Context);
            views.AddView(Resource.Id.widget_rows, row);
        }

        manager.UpdateAppWidget(widgetIds, views);
    }

    static PendingIntent? Open(Context context)
    {
        var intent = new Intent(context, typeof(MainActivity))
            .SetAction(ActionQuickAdd)
            .SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);

        // Immutable: nenhum outro app consegue alterar o intent depois de criado.
        return PendingIntent.GetActivity(context, 100, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
    }
}
