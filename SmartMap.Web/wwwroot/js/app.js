import ES6Lib from './lib';

import '../sass/app.scss';


let myES6Object = new ES6Lib();

document.getElementById("fillthis").innerHTML = myES6Object.getData();