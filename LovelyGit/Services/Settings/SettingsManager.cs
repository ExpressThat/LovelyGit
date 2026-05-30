using ExpressThat.LovelyGit.Services.Data;
using System.Text.Json;

namespace ExpressThat.LovelyGit.Services.Settings
{
    public class SettingsManager
    {
        private readonly AppDbContext _appDbContext;

        public SettingsManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<T> GetSetting<T>(SettingDefinition<T> setting)
        {
            var model = await _appDbContext.Settings.FindByIdAsync(setting.Name);
            if (model?.ValueJson == null)
            {
                return setting.DefaultValue;
            }

            return JsonSerializer.Deserialize(model.ValueJson, setting.JsonTypeInfo) ?? setting.DefaultValue;
        }

        public async Task<JsonElement> GetSettingValue(ISettingDefinition setting)
        {
            var model = await _appDbContext.Settings.FindByIdAsync(setting.Name);
            var valueJson = model?.ValueJson
                ?? JsonSerializer.Serialize(setting.DefaultValue, setting.JsonTypeInfo);

            using var document = JsonDocument.Parse(valueJson);
            return document.RootElement.Clone();
        }

        public async Task SetSettingValue(ISettingDefinition setting, string valueJson)
        {
            var typedValue = JsonSerializer.Deserialize(valueJson, setting.JsonTypeInfo);
            var model = new SettingModel
            {
                SettingName = setting.Name,
                ValueJson = JsonSerializer.Serialize(typedValue, setting.JsonTypeInfo),
            };

            var existing = await _appDbContext.Settings.FindByIdAsync(setting.Name);
            if (existing == null)
            {
                await _appDbContext.Settings.InsertAsync(model);
            }
            else
            {
                await _appDbContext.Settings.UpdateAsync(model);
            }

            await _appDbContext.SaveChangesAsync();
        }

        public async Task SetSetting<T>(SettingDefinition<T> setting, T value)
        {
            var model = new SettingModel
            {
                SettingName = setting.Name,
                ValueJson = JsonSerializer.Serialize(value, setting.JsonTypeInfo),
            };

            var existing = await _appDbContext.Settings.FindByIdAsync(setting.Name);
            if (existing == null)
            {
                await _appDbContext.Settings.InsertAsync(model);
            }
            else
            {
                await _appDbContext.Settings.UpdateAsync(model);
            }

            await _appDbContext.SaveChangesAsync();
        }
    }
}
