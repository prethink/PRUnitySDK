// Значок загрузки, который крутит страница, а не игра.
//
// Пока игра занята (сборка SDK, включение сцены), её собственный кадр не обновляется, и всё, что
// рисует Unity, замирает. Анимацию transform у отдельного элемента страницы браузер ведёт в своём
// потоке композитора: она идёт и тогда, когда главный поток занят игрой.
//
// Картинка приходит из игры пикселями и рисуется на маленьком <canvas>: так не нужна загрузка по
// ссылке, которую площадка могла бы запретить. Синтаксис ES5: jslib проходит через сборщик Emscripten.
mergeInto(LibraryManager.library,
{
	// rgba — пиксели RGBA снизу вверх, как их отдаёт Unity. centerX, centerY, size — доли холста игры:
	// центр значка и его сторона в долях высоты. turnSeconds — время одного оборота.
	PRBootSpinner_Show: function (rgbaPtr, width, height, centerX, centerY, size, turnSeconds)
	{
		try
		{
			var id = 'pr-boot-spinner';
			var old = document.getElementById(id);

			if (old && old.parentNode)
				old.parentNode.removeChild(old);

			if (!document.getElementById(id + '-style'))
			{
				var style = document.createElement('style');
				style.id = id + '-style';
				style.textContent = '@keyframes pr-boot-spin{from{transform:translate(-50%,-50%) rotate(0deg)}' +
					'to{transform:translate(-50%,-50%) rotate(360deg)}}';
				document.head.appendChild(style);
			}

			var element = document.createElement('canvas');
			element.id = id;
			element.width = width;
			element.height = height;

			// Строки переворачиваются: у Unity первая строка нижняя, у canvas — верхняя.
			var context = element.getContext('2d');
			var image = context.createImageData(width, height);
			var stride = width * 4;

			for (var row = 0; row < height; row++)
			{
				var from = rgbaPtr + (height - 1 - row) * stride;
				image.data.set(HEAPU8.subarray(from, from + stride), row * stride);
			}

			context.putImageData(image, 0, 0);

			var s = element.style;
			s.position = 'fixed';
			s.pointerEvents = 'none';
			s.zIndex = '2147483000';
			s.willChange = 'transform, opacity';
			s.opacity = '1';
			s.animation = 'pr-boot-spin ' + turnSeconds + 's linear infinite';

			// Место считается по холсту игры и пересчитывается при повороте экрана.
			var aspect = width / height;
			var place = function ()
			{
				var rect = Module.canvas.getBoundingClientRect();
				var side = size * rect.height;

				s.left = (rect.left + centerX * rect.width) + 'px';
				s.top = (rect.top + centerY * rect.height) + 'px';
				s.height = side + 'px';
				s.width = (side * aspect) + 'px';
			};

			place();
			element.prBootPlace = place;
			window.addEventListener('resize', place);
			document.body.appendChild(element);
		}
		catch (e)
		{
			console.error('Boot spinner failed: ' + (e && e.message));
		}
	},

	// Убирает значок, растворяя его за fadeSeconds.
	PRBootSpinner_Hide: function (fadeSeconds)
	{
		try
		{
			var element = document.getElementById('pr-boot-spinner');

			if (!element)
				return;

			if (element.prBootPlace)
				window.removeEventListener('resize', element.prBootPlace);

			var remove = function ()
			{
				if (element.parentNode)
					element.parentNode.removeChild(element);
			};

			if (fadeSeconds <= 0)
			{
				remove();
				return;
			}

			element.style.transition = 'opacity ' + fadeSeconds + 's linear';
			element.style.opacity = '0';
			setTimeout(remove, fadeSeconds * 1000 + 100);
		}
		catch (e)
		{
			console.error('Boot spinner hide failed: ' + (e && e.message));
		}
	}
});
