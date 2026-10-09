// Возврат фокуса браузера в игру после паузы площадки. Описание — в YandexFocusSync.cs.
mergeInto(LibraryManager.library, {

    // window.focus() отдаёт фрейму игры фокус окна браузера: одного canvas.focus() из шаблона
    // страницы для этого мало. Несколько попыток подряд — страница площадки в это время ещё
    // закрывает своё окно (оплаты, рекламы) и может вернуть фокус себе уже после первой.
    PRFocusGame_Request: function () {
        var state = Module.prFocusGame = Module.prFocusGame || { token: 0 };
        var token = ++state.token;
        var delays = [0, 250, 700, 1500];

        function focusGame() {
            // Новая пауза или новый запрос отменяют старые попытки.
            if (state.token !== token || document.hidden)
                return;

            // Игрок печатает в поле ввода — фокус у него не отбираем.
            var active = document.activeElement;
            if (active && (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA' || active.isContentEditable))
                return;

            try {
                if (!document.hasFocus())
                    window.focus();

                var canvas = Module.canvas || document.querySelector('canvas');
                if (canvas && document.activeElement !== canvas)
                    canvas.focus({ preventScroll: true });
            } catch (e) {
                console.warn('PRFocusGame: ' + e);
            }
        }

        for (var i = 0; i < delays.length; i++)
            setTimeout(focusGame, delays[i]);
    },

    PRFocusGame_Cancel: function () {
        var state = Module.prFocusGame = Module.prFocusGame || { token: 0 };
        state.token++;
    }
});
