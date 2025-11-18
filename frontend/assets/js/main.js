
fetch("/frontend/components/navbar/navbar.html")
    .then(res => res.text())
    .then(html => {
        document.getElementById("navbar-placeholder").innerHTML = html;
    });

fetch("/frontend/components/footer/footer.html")
    .then(res => res.text())
    .then(html => {
        document.getElementById("footer-placeholder").innerHTML = html;
    });
