using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear(); //Maak de lijst AvailableProducts leeg
            var AllProducts = _productService.GetAll(); //Haal de lijst met producten op
            foreach (var product in AllProducts)
            {
                if (product.Stock > 0 && //Houdt rekening met de voorraad (als die nul is kun je het niet meer aanbieden).     
                    !MyGroceryListItems.Any(item => item.ProductId == product.Id))
                {
                    AvailableProducts.Add(product);
                }
            }
            //Controleer of het product al op de boodschappenlijst staat, zo niet zet het in de AvailableProducts lijst
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }
        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product != null && product.Id > 0) //Controleer of het product bestaat en dat de Id > 0
            {
                //Maak een GroceryListItem met Id 0 en vul de juiste productid en grocerylistid
                var newItem = new GroceryListItem(
                    id: 0,
                    groceryListId: GroceryList.Id,
                    productId: product.Id,
                    amount: 1
                );

                //Voeg het GroceryListItem toe aan de dataset middels de _groceryListItemsService
                _groceryListItemsService.Add(newItem); 

                //Werk de voorraad (Stock) van het product bij en zorg dat deze wordt vastgelegd (middels _productService)
                product.Stock--;
                _productService.Update(product);

                GetAvailableProducts(); //Werk de lijst AvailableProducts bij, want dit product is niet meer beschikbaar
                OnGroceryListChanged(GroceryList); //call OnGroceryListChanged(GroceryList);
            }
        }
    }
}
