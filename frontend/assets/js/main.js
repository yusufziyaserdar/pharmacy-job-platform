// Navbar yükle
fetch("/frontend/components/navbar/navbar.html")
    .then(response => response.text())
    .then(data => {
        document.getElementById("navbar-placeholder").innerHTML = data;
    });

// Footer yükle
fetch("/frontend/components/footer/footer.html")
    .then(response => response.text())
    .then(data => {
        document.getElementById("footer-placeholder").innerHTML = data;
    });
