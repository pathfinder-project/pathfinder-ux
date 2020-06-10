from flask import Flask
from flask import request
from flask import render_template
import os
from datetime import datetime
from skimage.io import imread, imsave
from skimage.color import rgba2rgb, rgb2hed, rgb2lab
from skimage.draw import polygon
from skimage.exposure import rescale_intensity
import skimage.filters as skf
import numpy as np
import matplotlib.pyplot as plt


app = Flask(__name__)


@app.route('/')
def index():
    return 'it works'


@app.route('/ki67', methods=['POST'])
def ki67():
    img_raw = request.files['img']
    img_np = rgba2rgb(imread(img_raw))
    
    x_raw = request.form['x_coordinates']
    c_np = np.array(x_raw.split(), dtype=np.float)
    c_np = c_np - np.min(c_np)

    y_raw = request.form['y_coordinates']
    r_np = np.array(y_raw.split(), dtype=np.float)
    r_np = r_np - np.min(r_np)
    
    score = ki67_algorithm(img_np, r_np, c_np)
    print(score)
    return str(score)
    

def ki67_algorithm(img, r, c):
    mask = np.ones_like(img)
    rr, cc = polygon(r, c)
    mask[rr, cc] = 0
    img = np.maximum(img, mask)
    # imsave("hard.png", img)
    hed = rgb2hed(img)
    h = rescale_intensity(hed[:,:,0], out_range=(0, 1))
    d = rescale_intensity(hed[:,:,2], out_range=(0, 1))
    # hd = np.maximum(h, d)
    h_bin= h >= skf.threshold_yen(h)
    d_bin = d >= skf.threshold_yen(d)
    hd_bin = np.maximum(h_bin, d_bin)
    plt.imshow(img)
    plt.show()
    plt.imshow(hd_bin)
    plt.show()
    plt.imshow(d_bin)
    plt.show()
    return np.sum(d_bin) * 1.0 / np.sum(hd_bin)


def now():
    return datetime.now().strftime('%Y.%m.%d_%H.%M.%S')


def main():
    app.run(host = '0.0.0.0', port = 1919, debug = False)
    # img = imread('hard.png')
    # hed = rgb2hed(img)
    # plt.imshow(img); plt.suptitle('Input'); plt.show()
    # hd = rescale_intensity(hed[:,:,0], out_range=(0, 1))
    # d = rescale_intensity(hed[:,:,2], out_range=(0, 1))
    #
    # plt.imshow(hd); plt.suptitle('h+d intensity'); plt.show()
    # for th in [skf.threshold_otsu, skf.threshold_yen, skf.threshold_li, skf.threshold_isodata,
    #            skf.threshold_mean, skf.threshold_triangle, skf.threshold_minimum]:
    #     global_th_test(hd, d, th)


def global_th_test(hd, d, th):
    hd_bin = hd >= th(hd)
    plt.imshow(hd_bin); plt.suptitle('Hema+DAB {0}'.format(th.__name__)); plt.show()
    d_bin = d >= th(d)
    plt.imshow(d_bin); plt.suptitle('DAB {0}'.format(th.__name__)); plt.show()
    

if __name__ == '__main__':
    main()