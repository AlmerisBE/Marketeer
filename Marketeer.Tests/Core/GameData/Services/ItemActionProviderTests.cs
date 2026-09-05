using Dalamud.Plugin.Services;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.GameData.Services;

public class ItemActionProviderTests {
    private IDataManager dataManager;
    private IGameGui gameGui;
    private IRecipeDataService recipeDataService;
    private ItemActionProvider provider;

    public ItemActionProviderTests() {
        this.dataManager = Substitute.For<IDataManager>();
        this.gameGui = Substitute.For<IGameGui>();
        this.recipeDataService = Substitute.For<IRecipeDataService>();

        this.provider = new ItemActionProvider(this.dataManager, this.gameGui, this.recipeDataService);
    }

    [Fact]
    public void CanOpenRecipe_ShouldReturnTrue_WhenItemIsCraftable() {
        this.recipeDataService.IsCraftable(123).Returns(true);
        var result = this.provider.CanOpenRecipe(123);
        Assert.True(result);
    }

    [Fact]
    public void CanOpenRecipe_ShouldReturnFalse_WhenItemIsNotCraftable() {
        this.recipeDataService.IsCraftable(456).Returns(false);
        var result = this.provider.CanOpenRecipe(456);
        Assert.False(result);
    }
}