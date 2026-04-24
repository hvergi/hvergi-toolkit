//List of meals access by following Affinty, Station
var ListofMeals = [];
var i;
for(i=0; i<138; i++){
    ListofMeals[i]=[];
}
//Expected output from array is in this order
//station,Meat,cheese,largeveggie,med,herb,small

window.addEventListener('DOMContentLoaded', function() {
    setTimeout(function(){        
        this.Setup();
    }, 1000)
 }, false);

 function timer(ms) {
    return new Promise(res => setTimeout(res, ms));
}


//CookingStation.length  limiting station for loading speed
async function Setup(){
    var IterStation; var IterMeat; var IterCheese; var IterLarge; var IterMed; var IterHerb;
    var barcount=0;
    for(IterStation=0; IterStation<CookingStation.length; IterStation++){
        for(IterMeat=0; IterMeat<MeatList.length; IterMeat++){
            var progress = barcount/(CookingStation.length * MeatList.length) * 100;
            barcount+=1;
            document.getElementById("ProgressBar").style = "width:"+progress+"%";
            document.getElementById("ProgressBar").innerHTML = progress.toFixed(1)+"%";
            await timer(10);
            for(IterCheese=0; IterCheese<CheeseList.length; IterCheese++){
                for(IterLarge=0; IterLarge<VeggiesLarge.length; IterLarge++){
                    for(IterMed=0; IterMed<VeggiesMediumRange.length; IterMed++){
                        for(IterHerb=0; IterHerb<HerbsRange.length; IterHerb++){
                        var Value = 0;
                        var fooditem = [];
                        Value+=CookingStation[IterStation];
                        Value+=CookingContainer;
                        fooditem[0]=IterStation;

                        Value+=MeatList[IterMeat] + Minced;
                        fooditem[1]=IterMeat;
                        
                        Value+=CheeseList[IterCheese];
                        fooditem[2]=IterCheese;

                        Value+=VeggiesLarge[IterLarge]+Chopped;
                        fooditem[3]=IterLarge;
                        
                        var K; var spot =4;
                        for(K=0; K < VeggiesMediumRange[IterMed].length; K++){
                            Value+= VeggiesMedium[VeggiesMediumRange[IterMed][K]] + Chopped;
                            fooditem[spot]=VeggiesMediumRange[IterMed][K];
                            spot+=1;
                        }
                        for(K=0; K < HerbsRange[IterHerb].length; K++){
                            Value+= Herbs[HerbsRange[IterHerb][K]] + Chopped;
                            fooditem[spot]=HerbsRange[IterHerb][K];
                            spot+=1;
                        }
                        for(K=0; K < VeggiesSmall.length; K++){
                            Value+= VeggiesSmall[K] + Chopped;
                            fooditem[spot]=K;
                            spot+=1;
                        }
                        ListofMeals[Value%138].push(fooditem);
                        
                        
                        
                        }
                    }
                }
            }
        }
    }
    document.getElementById("LoadingScreen").classList.remove("fullscreen");
    document.getElementById("LoadingScreen").classList.add("hidden");
}