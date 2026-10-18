// Plano del salón: posiciona las mesas en el lienzo (unidades 1200 x 800) y dibuja las sillas
(function () {
    var ANCHO = 1200, ALTO = 800;

    function num(v, d) { var n = parseInt(v, 10); return isNaN(n) ? d : n; }

    // Ubica la mesa usando porcentajes del lienzo para que escale con la pantalla
    function aplicar(el) {
        var d = el.dataset;
        el.style.left = (num(d.x, 0) / ANCHO * 100) + '%';
        el.style.top = (num(d.y, 0) / ALTO * 100) + '%';
        el.style.width = (num(d.w, 90) / ANCHO * 100) + '%';
        el.style.height = (num(d.h, 90) / ALTO * 100) + '%';
        el.style.transform = 'rotate(' + num(d.rot, 0) + 'deg)';
        var info = el.querySelector('.pm-info');
        if (info) info.style.transform = 'rotate(' + (-num(d.rot, 0)) + 'deg)';
    }

    // Reparte las sillas alrededor de la mesa según su forma y capacidad
    function sillas(el) {
        el.querySelectorAll('.pm-silla').forEach(function (s) { s.remove(); });
        var n = Math.max(0, Math.min(num(el.dataset.cap, 4), 24));
        var forma = el.dataset.forma || 'cuadrada';
        var pos = [];
        if (forma === 'redonda') {
            for (var i = 0; i < n; i++) {
                var a = (i / n) * Math.PI * 2 - Math.PI / 2;
                pos.push([50 + Math.cos(a) * 62, 50 + Math.sin(a) * 62]);
            }
        } else {
            // Lados: arriba, abajo, izquierda, derecha. Las rectangulares priorizan los lados largos.
            var lados = [[], [], [], []];
            var orden = forma === 'rectangular' ? [0, 1, 0, 1, 2, 3] : [0, 1, 2, 3];
            for (var j = 0; j < n; j++) lados[orden[j % orden.length]].push(j);
            lados.forEach(function (l, lado) {
                l.forEach(function (_, k) {
                    var t = (k + 1) / (l.length + 1) * 100;
                    if (lado === 0) pos.push([t, -14]);
                    if (lado === 1) pos.push([t, 114]);
                    if (lado === 2) pos.push([-14, t]);
                    if (lado === 3) pos.push([114, t]);
                });
            });
        }
        pos.forEach(function (p) {
            var s = document.createElement('span');
            s.className = 'pm-silla';
            s.style.left = p[0] + '%';
            s.style.top = p[1] + '%';
            el.appendChild(s);
        });
    }

    // Las mesas que aún no tienen posición se acomodan en una grilla libre
    function ubicarPendientes(lienzo) {
        var pendientes = Array.prototype.filter.call(lienzo.querySelectorAll('.pm-mesa'), function (m) { return m.dataset.x === ''; });
        var col = 0, fila = 0, paso = 170;
        pendientes.forEach(function (m) {
            m.dataset.x = 40 + col * paso;
            m.dataset.y = 50 + fila * 170;
            m.dataset.pendiente = '1';
            col++;
            if (40 + (col + 1) * paso > ANCHO) { col = 0; fila++; }
        });
        return pendientes.length;
    }

    function iniciar(lienzo) {
        ubicarPendientes(lienzo);
        lienzo.querySelectorAll('.pm-mesa').forEach(function (m) { aplicar(m); sillas(m); });
        lienzo.classList.toggle('vacio-plano', !lienzo.querySelector('.pm-mesa'));
    }

    window.Plano = { ANCHO: ANCHO, ALTO: ALTO, aplicar: aplicar, sillas: sillas, iniciar: iniciar };
})();
