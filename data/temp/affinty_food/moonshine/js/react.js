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
    var selTest = document.getElementById("TestCurrent");


    var currentAffinty = selCur.options[selCur.selectedIndex].value
    var desiredAffinty = selWant.options[selWant.selectedIndex].value
    var testAffinty = selTest.options[selTest.selectedIndex].value

    var PlayerOffset = desiredAffinty - currentAffinty + 37
    if (PlayerOffset < 0){
        PlayerOffset = 138 + PlayerOffset;
    }
    PlayerOffset = PlayerOffset % 138

    console.log((currentAffinty-testAffinty)%138)
    var selValue = -1
    for (var i=0; i<selTest.options.length; i++){
        var TestOffset = selTest.options[i].value - testAffinty + 37
        if (TestOffset < 0)
            TestOffset = 138 + TestOffset
        TestOffset = TestOffset % 138

        if (TestOffset == PlayerOffset){
            document.getElementById("TestTarget").innerHTML = selTest.options[i].text
            console.log(selTest.options[i].text)
            break;
        }
    }

    
    addRecipes(PlayerOffset);
    
    
    //var divDebug = document.getElementById("debug");
    //divDebug.innerHTML="Drink From Barrel: " + PlayerOffset%138
}






function addRecipes(ID){
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
    CSTitle.appendChild(document.createTextNode("Drink From Barrel: "))
    divItem.appendChild(CSTitle);
    divItem.appendChild(document.createTextNode(ID));
    MealContainer.appendChild(divItem);
    

    var divItem = document.createElement("div");
    divItem.classList.add("w3-container");
    var CSTitle = document.createElement("strong");
    CSTitle.appendChild(document.createTextNode("Cooking Station: "))
    divItem.appendChild(CSTitle);
    divItem.appendChild(document.createTextNode(CookingStationName));
    MealContainer.appendChild(divItem);
        
    var divItem = document.createElement("div");
    divItem.classList.add("w3-container");
    var CSTitle = document.createElement("strong");
    CSTitle.appendChild(document.createTextNode("Cooking Container: "))
    divItem.appendChild(CSTitle);
    divItem.appendChild(document.createTextNode(CookingContainerName));
    MealContainer.appendChild(divItem);

    var divItem = document.createElement("div");
    divItem.classList.add("w3-container");
    var CSTitle = document.createElement("strong");
    CSTitle.appendChild(document.createTextNode("Grain: "))
    divItem.appendChild(CSTitle);
    divItem.appendChild(document.createTextNode(GrainListNames[moonshineList[ID][0]]));
    MealContainer.appendChild(divItem);

    var divItem = document.createElement("div");
    divItem.classList.add("w3-container");
    var CSTitle = document.createElement("strong");
    CSTitle.appendChild(document.createTextNode("Water: "))
    divItem.appendChild(CSTitle);

    divItem.appendChild(document.createTextNode(WaterListNames[moonshineList[ID][1]]));
    MealContainer.appendChild(divItem);

    var divItem = document.createElement("div");
    divItem.classList.add("w3-container");
    var CSTitle = document.createElement("strong");
    CSTitle.appendChild(document.createTextNode("Sugar: "))
    divItem.appendChild(CSTitle);

    divItem.appendChild(document.createTextNode(moonshineList[ID][2]/47 + " sugar"));
    MealContainer.appendChild(divItem);
    
    var divItem = document.createElement("div");
    divItem.classList.add("w3-container");
    var CSTitle = document.createElement("strong");
    CSTitle.appendChild(document.createTextNode("Veg: "))
    divItem.appendChild(CSTitle);

    divItem.appendChild(document.createTextNode(VegStateNames[moonshineList[ID][4]] + VegListNames[moonshineList[ID][3]]));
    MealContainer.appendChild(divItem);
        
        
    document.getElementById("Meals").appendChild(MealContainer);		
}