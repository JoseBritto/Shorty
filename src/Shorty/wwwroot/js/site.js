let menu_btn;
let mobile_drawer;
let mobile_nav;
let container;

window.onload = function() {
    menu_btn = document.getElementById('menu-btn');
    mobile_drawer = document.getElementById('mobile-drawer');
    mobile_nav = document.getElementById('mobile-nav');
    container = document.getElementById('container-noboot-shrinker');
    
    window.addEventListener('click', function (e) {
        if (!mobile_drawer.contains(e.target)) {
            menu_btn.classList.remove('open');
            mobile_nav.classList.remove('slide-in');
            mobile_nav.classList.add('slide-out');
            container.classList.remove('shrink');
        }
    });
}

function shrink_content() {
    menu_btn.classList.add('open');
    mobile_nav.classList.add('slide-in');
    mobile_nav.classList.remove('slide-out');
    container.classList.add('shrink');
}

function expand_content(){
    menu_btn.classList.remove('open');
    mobile_nav.classList.remove('slide-in');
    mobile_nav.classList.add('slide-out');
    container.classList.remove('shrink');
}

function toggle_shrink(){
    if(menu_btn.classList.contains('open'))
        expand_content();
    else 
        shrink_content();
}