


function pickQL(){
    var selMining = document.getElementById("MiningLvl");
    var selVein = document.getElementById("VeinSelection");
    var miningLvl = selMining.value;
    var vienDiff = parseInt(selVein.options[selVein.selectedIndex].value);

    var targetPickQl = ((vienDiff+20)*2)-miningLvl;

    document.getElementById("PickQL").innerHTML=targetPickQl;
}
