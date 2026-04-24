var Fails=0;

function resetMeals(){
    const myNode = document.getElementById("Meals");
     while (myNode.firstChild) {
        myNode.removeChild(myNode.lastChild);
    }
}

function GetMeals(){
    resetMeals();
    var selCur = document.getElementById("Current");
    var selWant = document.getElementById("Wanted");
    var currentAffinty = selCur.options[selCur.selectedIndex].value
    var desiredAffinty = selWant.options[selWant.selectedIndex].value
    var PlayerOffset = desiredAffinty - currentAffinty + 37
    //console.log(PlayerOffset);
    if (PlayerOffset < 0)
        PlayerOffset = 138 + PlayerOffset;
    
    
    
    addRecipes(PlayerOffset);
}






async function addRecipes(ID){
    var i = 0;
    var itemCount = 0;
    var selStation = document.getElementById("DesiredStation")
    var validStation = selStation.options[selStation.selectedIndex].value;

    var selCheese = document.getElementById("DesiredCheese")
    var validCheese = selCheese.options[selCheese.selectedIndex].value;

    var IsBear = !document.getElementById("Bear").checked;
    var IsBeef = !document.getElementById("Beef").checked;
    var IsCanine = !document.getElementById("Canine").checked;
    var IsFeline = !document.getElementById("Feline").checked;
    var IsFowl = !document.getElementById("Fowl").checked;
    var IsGame = !document.getElementById("Game").checked;
    var IsHorse = !document.getElementById("Horse").checked;
    var IsHumanoid = !document.getElementById("Humanoid").checked;
    var IsInsect = !document.getElementById("Insect").checked;
    var IsLamb = !document.getElementById("Lamb").checked;
    var IsPork = !document.getElementById("Pork").checked;
    var IsSeafood = !document.getElementById("Seafood").checked;
    var IsSnake = !document.getElementById("Snake").checked;
    var IsTough = !document.getElementById("Tough").checked;

    var IsBasil = !document.getElementById("Basil").checked;
    var IsBelladonna = !document.getElementById("Belladonna").checked;
    var IsCumin = !document.getElementById("Cumin").checked;
    
    var IsFennelSeed = !document.getElementById("FennelSeed").checked;
    var IsGinger = !document.getElementById("Ginger").checked;
    var IsLoveage = !document.getElementById("Loveage").checked;
    var IsMint = !document.getElementById("Mint").checked;
    var IsOregano = !document.getElementById("Oregano").checked;
    var IsPaprika = !document.getElementById("Paprika").checked;
    var IsParsley = !document.getElementById("Parsley").checked;
    var IsRosemary = !document.getElementById("Rosemary").checked;
    var IsSage = !document.getElementById("Sage").checked;
    var IsThyme = !document.getElementById("Thyme").checked;
    var IsTumeric = !document.getElementById("Tumeric").checked;
    
    var IsSalt = document.getElementById("Salt").checked;
    
    
    var spiceCounter = 0;
    
    if(IsSalt){
        ID-=99;
        if(ID < 0)
            ID = ID + 138
    }
	
  	ID = ID % 138; //Testing
    for(i=0; i<ListofMeals[ID].length; i++){
        
        
        //Tater filter becase the blasted thing broke spices;
        if(ListofMeals[ID][i][4]==2 || ListofMeals[ID][i][5]==2 || ListofMeals[ID][i][6]==2){
            spiceCounter=0;
            if(ListofMeals[ID][i][7]==1 || ListofMeals[ID][i][7]==4 || ListofMeals[ID][i][7]==5 || ListofMeals[ID][i][7]==6 || ListofMeals[ID][i][7]==13)
                spiceCounter++;
            if(ListofMeals[ID][i][7]==1 || ListofMeals[ID][i][7]==4 || ListofMeals[ID][i][7]==5 || ListofMeals[ID][i][7]==6 || ListofMeals[ID][i][7]==13)
                spiceCounter++;
            if(ListofMeals[ID][i][7]==1 || ListofMeals[ID][i][7]==4 || ListofMeals[ID][i][7]==5 || ListofMeals[ID][i][7]==6 || ListofMeals[ID][i][7]==13)
                spiceCounter++;

            if(spiceCounter>1){
                Fails++;
                continue;
            }
        }

        if(validStation != ListofMeals[ID][i][0])
            continue;
        if(validCheese != ListofMeals[ID][i][2])
            continue;

        if(IsBear && ListofMeals[ID][i][1]==0)
            continue;
        if(IsBeef && ListofMeals[ID][i][1]==1)
            continue;
        if(IsCanine && ListofMeals[ID][i][1]==2)
            continue;
        if(IsFeline && ListofMeals[ID][i][1]==3)
            continue;
        if(IsFowl && ListofMeals[ID][i][1]==4)
            continue;
        if(IsGame && ListofMeals[ID][i][1]==5)
            continue;
        if(IsHorse && ListofMeals[ID][i][1]==6)
            continue;
        if(IsHumanoid && ListofMeals[ID][i][1]==7)
            continue;
        if(IsInsect && ListofMeals[ID][i][1]==8)
            continue;
        if(IsLamb && ListofMeals[ID][i][1]==9)
            continue;
        if(IsPork && ListofMeals[ID][i][1]==10)
            continue;
        if(IsSeafood && ListofMeals[ID][i][1]==11)
            continue;
        if(IsSnake && ListofMeals[ID][i][1]==12)
            continue;
        if(IsTough && ListofMeals[ID][i][1]==13)
            continue;


        if(IsBasil &&  (ListofMeals[ID][i][7]==11 || ListofMeals[ID][i][8]==11 || ListofMeals[ID][i][9]==11))
            continue;
        if(IsBelladonna &&  (ListofMeals[ID][i][7]==7 || ListofMeals[ID][i][8]==7 || ListofMeals[ID][i][9]==7))
            continue;
        if(IsCumin &&  (ListofMeals[ID][i][7]==5 || ListofMeals[ID][i][8]==5 || ListofMeals[ID][i][9]==5))
            continue;
        if(IsFennelSeed &&  (ListofMeals[ID][i][7]==13 || ListofMeals[ID][i][8]==13 || ListofMeals[ID][i][9]==13))
            continue;
        if(IsGinger &&  (ListofMeals[ID][i][7]==4 || ListofMeals[ID][i][8]==4 || ListofMeals[ID][i][9]==4))
            continue;
        if(IsLoveage &&  (ListofMeals[ID][i][7]==8 || ListofMeals[ID][i][8]==8 || ListofMeals[ID][i][9]==8))
            continue;
        if(IsMint &&  (ListofMeals[ID][i][7]==10 || ListofMeals[ID][i][8]==10 || ListofMeals[ID][i][9]==10))
            continue;
        if(IsOregano &&  (ListofMeals[ID][i][7]==12 || ListofMeals[ID][i][8]==12 || ListofMeals[ID][i][9]==12))
            continue;
        if(IsPaprika &&  (ListofMeals[ID][i][7]==1 || ListofMeals[ID][i][8]==1 || ListofMeals[ID][i][9]==1))
            continue;
        if(IsParsley &&  (ListofMeals[ID][i][7]==2 || ListofMeals[ID][i][8]==2 || ListofMeals[ID][i][9]==2))
            continue;
        if(IsRosemary &&  (ListofMeals[ID][i][7]==9 || ListofMeals[ID][i][8]==9 || ListofMeals[ID][i][9]==9))
            continue;
        if(IsSage &&  (ListofMeals[ID][i][7]==3 || ListofMeals[ID][i][8]==3 || ListofMeals[ID][i][9]==3))
            continue;
        if(IsThyme &&  (ListofMeals[ID][i][7]==0 || ListofMeals[ID][i][8]==0 || ListofMeals[ID][i][9]==0))
            continue;
        if(IsTumeric &&  (ListofMeals[ID][i][7]==6 || ListofMeals[ID][i][8]==6 || ListofMeals[ID][i][9]==6))
            continue;
        
        
        itemCount++;
        //console.log(itemCount);
        if (itemCount>20) {
            break;     
        }
        


        var MealContainer = document.createElement("div");
        MealContainer.classList.add("w3-card-4");
        MealContainer.classList.add("l3");
        MealContainer.classList.add("w3-col");
        MealContainer.classList.add("w3-padding");
        MealContainer.classList.add("w3-margin")
        MealContainer.classList.add("w3-theme-l5");
        
        var divItem = document.createElement("div");
        divItem.classList.add("w3-container");
        var CSTitle = document.createElement("strong");
        CSTitle.appendChild(document.createTextNode("Cooking Station: "))
        divItem.appendChild(CSTitle);
        divItem.appendChild(document.createElement("br"));
        divItem.appendChild(document.createTextNode(CookingStationNames[ListofMeals[ID][i][0]]));
        MealContainer.appendChild(divItem);
        
        var divItem = document.createElement("div");
        divItem.classList.add("w3-container");
        var CSTitle = document.createElement("strong");
        CSTitle.appendChild(document.createTextNode("Cooking Container: "))
        divItem.appendChild(CSTitle);
        divItem.appendChild(document.createElement("br"));
        divItem.appendChild(document.createTextNode(CookingContainerName));
        MealContainer.appendChild(divItem);

        var divItem = document.createElement("div");
        divItem.classList.add("w3-panel");
        var CSTitle = document.createElement("strong");
        CSTitle.appendChild(document.createTextNode("Meat: "))
        divItem.appendChild(CSTitle);
        divItem.appendChild(document.createElement("br"));
        divItem.appendChild(document.createTextNode(MeatListNames[ListofMeals[ID][i][1]]));
        MealContainer.appendChild(divItem);

        var divItem = document.createElement("div");
        divItem.classList.add("w3-panel");
        var CSTitle = document.createElement("strong");
        CSTitle.appendChild(document.createTextNode("Cheese: "))
        divItem.appendChild(CSTitle);
        divItem.appendChild(document.createElement("br"));
        divItem.appendChild(document.createTextNode(CheeseListNames[ListofMeals[ID][i][2]]));
        MealContainer.appendChild(divItem);			
        
        var divItem = document.createElement("div");
        divItem.classList.add("w3-container");
        var CSTitle = document.createElement("strong");
        CSTitle.appendChild(document.createTextNode("Herbs/Spices:"))
        divItem.appendChild(CSTitle);
        var ulList = document.createElement("ul");
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(HerbsNames[ListofMeals[ID][i][7]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(HerbsNames[ListofMeals[ID][i][8]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(HerbsNames[ListofMeals[ID][i][9]]));
        ulList.appendChild(liItem);
        if(IsSalt){
            var liItem = document.createElement("li");
            liItem.appendChild(document.createTextNode("Salt"));
            ulList.appendChild(liItem);
        }
        divItem.appendChild(ulList);
        MealContainer.appendChild(divItem);

        var divItem = document.createElement("div");
        divItem.classList.add("w3-container");
        var CSTitle = document.createElement("strong");
        CSTitle.appendChild(document.createTextNode("Veggies:"))
        divItem.appendChild(CSTitle);
        var ulList = document.createElement("ul");
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesLargeNames[ListofMeals[ID][i][3]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesMediumNames[ListofMeals[ID][i][4]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesMediumNames[ListofMeals[ID][i][5]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesMediumNames[ListofMeals[ID][i][6]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesSmallNames[ListofMeals[ID][i][10]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesSmallNames[ListofMeals[ID][i][11]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesSmallNames[ListofMeals[ID][i][12]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesSmallNames[ListofMeals[ID][i][13]]));
        ulList.appendChild(liItem);
        var liItem = document.createElement("li");
        liItem.appendChild(document.createTextNode(VeggiesSmallNames[ListofMeals[ID][i][14]]));
        ulList.appendChild(liItem);
        divItem.appendChild(ulList);
        MealContainer.appendChild(divItem);	
        
        
        
        
        
        
        

        document.getElementById("Meals").appendChild(MealContainer);
        await timer(10);			
    }
}